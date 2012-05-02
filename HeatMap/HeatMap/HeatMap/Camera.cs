#region Using Statements

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Engine.Utility;

#endregion


namespace Engine
{
    public interface IPositionable
    {
        Vector2 GetPosition();
    }

    /// <summary>
    /// The modes that a camera can be in.
    /// 
    /// LockedPos     :: Locked to a single Vector2, does not move
    /// LockedTarget  :: Locked to a single GameObject, tracks the center of the screen to GameObject.Position
    /// VtTTransition :: Vector -> GameObject Transition.  Moving from a LockedPos to LockedTarget
    /// TtVTransition :: GameObject -> Vector Transition.  Moving from a LockedTarget to LockedPos
    /// TtTTransition :: GameObject -> GameObject Transition.  Moving from one LockedPos to another
    /// VtVTransition :: Vector -> Vector Transition.  Moving from one LockedPos to another
    /// </summary>
    public enum CameraMode { LockedPos, LockedTarget, VtTTransition, TtVTransition, TtTTransition, VtVTransition };

    #region CameraEffects

    /// <summary>
    /// Modifies the position, scale, or rotation of a camera.
    /// Care should be taken that a CameraEffect never lets net scale = 0
    /// </summary>
    public class CameraEffect
    {
        protected Camera camera; 
        protected float elapsed;
        protected float dur;

        /// <summary>
        /// Percent of effect elapsed- t in [0, 1]
        /// </summary>
        protected float t 
        { 
            get 
            {
                if (dur == 0)
                    return 0;
                return MathHelper.Clamp(elapsed / dur, 0, 1); 
            } 
        }

        public bool IsActive
        {
            get { return dur > 0 && elapsed <= dur; }
        }

        public CameraEffect(Camera camera, float dur)
        {
            elapsed = 0;
            this.camera = camera; 
            this.dur = dur / 1000f;
        }

        public virtual void Update(float dt)
        {
            elapsed += dt;
        }

        public virtual void Reset()
        {
            elapsed = 0;
        }

        public virtual void End()
        {
            elapsed = dur = 0;
            camera = null;
        }

        public float[] Offsets
        {
            get
            {
                float[] offsets = new float[5];
                offsets[0] = offsetX();
                offsets[1] = offsetY();
                offsets[2] = rotation();
                offsets[3] = scaleX();
                offsets[4] = scaleY();
                return offsets;
            }
        }

        protected virtual float offsetX() { return 0; }
        protected virtual float offsetY() { return 0; }
        
        /// <summary>
        /// Always multiply by camera scale so that we don't hit a scale of 0.
        /// Original equation was scale *= (1 + offsetScale)
        /// New equation is scale += offsetScale
        /// We can keep the functionality by ultiplying by camera scale in this equation
        /// </summary>
        /// <returns></returns>
        protected virtual float scaleX() { return 0; }

        /// <summary>
        /// Always multiply by camera scale so that we don't hit a scale of 0.
        /// Original equation was scale *= (1 + offsetScale)
        /// New equation is scale += offsetScale
        /// We can keep the functionality by ultiplying by camera scale in this equation
        /// </summary>
        /// <returns></returns>
        protected virtual float scaleY() { return 0; }
        
        protected virtual float rotation() { return 0; }
    }

    /// <summary>
    /// This effect doesn't impact the camera in any way
    /// However, it keeps the CameraEffect cleaner (not all effects need freq, decay, mag)
    /// and prevents repitition of variables in subclasses (Shake, Bounce)
    /// </summary>
    public class DampedOscillationEffect : CameraEffect
    {
        protected float decay;
        protected float mag;
        protected float freq;

        public DampedOscillationEffect(Camera camera, float dur, float decay, float mag, float freq)
            : base(camera, dur)
        {
            this.decay = decay;
            this.mag = mag;
            this.freq = freq;
        }
    }

    /// <summary>
    /// Creates a radial shake effect, using damped oscillation
    /// One pass goes from 0 -> mag -> 0 -> -mag -> 0
    /// Specify the number of passes to make via frequency
    /// </summary>
    public class CameraShakeEffect : DampedOscillationEffect
    {
        protected MathFunction magFunction;
        protected MathFunction oscillateFunction;

        /// <summary>
        /// Applys a rotation effect that oscillates from -mag to +mag and decays over time.
        /// </summary>
        /// <param name="camera">The camera the effect is applied to</param>
        /// <param name="dur">Duration in ms for the effect to occur over</param>
        /// <param name="decay">Decay coeffecient of the shaking.  Use 0 for no decay</param>
        /// <param name="mag">Maximum magnitude in radians for the camera to rotate</param>
        /// <param name="freq">Number of times the camera makes a full left -> right -> left pass</param>
        public CameraShakeEffect(Camera camera, float dur, float decay, float mag, float freq)
            : base(camera, dur, decay, mag, freq) 
        {
            magFunction = new DecayFunction(0, 1, decay);
            oscillateFunction = new OscillatingFunction(0, 1, freq);
        }

        protected override float rotation()
        {
            return mag * magFunction.At(t) * oscillateFunction.At(t);
        }

    }
    
    /// <summary>
    /// Creates a 'pop' or bounce effect, using damped oscillation
    /// One pass goes from 0 -> cameraScale * mag -> 0 -> -cameraScale * mag -> 0
    /// Specify the number of passes to make via frequency
    /// </summary>
    public class CameraBounceEffect : DampedOscillationEffect
    {
        protected MathFunction magFunction;
        protected MathFunction oscillateFunction;

        /// <summary>
        /// Applys a scale effect that oscillates from (1 - mag) to (1 + mag) and decays over time.
        /// </summary>
        /// <param name="camera">The camera the effect is applied to</param>
        /// <param name="dur">Duration in ms for the effect to occur over</param>
        /// <param name="decay">Decay coeffecient of the zoom.  Use 0 for no decay</param>
        /// <param name="mag">Maximum magnitude in percent original zoom to move by</param>
        /// <param name="freq">Number of times the camera makes a full in -> out -> in pass</param>
        public CameraBounceEffect(Camera camera, float dur, float decay, float mag, float freq)
            : base(camera, dur, decay, mag, freq)
        {
            magFunction = new DecayFunction(0, 1, decay);
            oscillateFunction = new OscillatingFunction(0, 1, freq);
        }

        protected override float scaleX()
        {
            return camera.Scale.X * scale();
        }
        protected override float scaleY()
        {
            return camera.Scale.Y * scale();
        }
        private float scale()
        {
            return mag * magFunction.At(t) * oscillateFunction.At(t);
        }

    }

    /// <summary>
    /// Flips the camera about its x- or y-axis, or both.
    /// Because this effect modifies camera scale directly, 
    /// take care when combining with other effects that modify scale.
    /// </summary>
    public class CameraFlipEffect : CameraEffect
    {
        protected Vector2 tempScale;
        protected bool flipX, flipY;
        protected float adjust;

        public CameraFlipEffect(Camera camera, float dur, bool flipX, bool flipY) : base(camera, dur)
        {
            adjust = -1f;

            this.flipX = flipX;
            this.flipY = flipY;

            SetScale(adjust);
        }

        public override void End()
        {
            SetScale(1 / adjust);
            base.End();
        }

        private void SetScale(float scale)
        {
            tempScale = camera.Scale;
            if (flipX)
                tempScale.X *= scale;
            if (flipY)
                tempScale.Y *= scale;
            camera.Scale = tempScale;
        }
    }

    #endregion

    /// <summary>
    /// A 2D camera with a small list of effects, 
    /// allows easy tracking of objects and transitions between
    /// focus points.
    /// </summary>
    public class Camera
    {
        #region Fields

        /// <summary>
        /// True if information used to calc the transform matrix 
        /// has changed since it was last calculated
        /// </summary>
        protected bool isTransformDirty;
        
        /// <summary>
        /// Most recently calculated transform matrix- use isTransformDirty to see if value is still correct
        /// </summary>
        protected Matrix transformMatrix;

        /// <summary>
        /// The current camera mode for this camera.  Can be locked, transition, etc.
        /// </summary>
        protected CameraMode mode;

        /// <summary>
        /// The viewport that this camera maps world-space to
        /// </summary>
        protected Viewport viewport;

        /// <summary>
        /// The current GameObject target.  Can be null when not in LockedTarget mode
        /// </summary>
        protected IPositionable target;

        /// <summary>
        /// The GameObject the camera was last locked to.  Used for smooth transitions
        /// </summary>
        protected IPositionable oldTarget;

        /// <summary>
        /// The current Vector2 target.  Can be null when not in LockedPos mode
        /// </summary>
        protected Vector2 targetPos;

        /// <summary>
        /// The Vector2 the camera was last locked to.  Used for smooth transitions
        /// </summary>
        protected Vector2 oldTargetPos;

        /// <summary>
        /// Camera rotation in radians.
        /// </summary>
        protected float rotation;
        
        /// <summary>
        /// Camera rotation in radians.
        /// </summary>
        public float Rotation
        {
            get { return rotation; }
            set 
            { 
                rotation = value;
                isTransformDirty = true;
            }
        }

        /// <summary>
        /// Camera scale- 1.0 is 100%, 2.0 is 200%, etc
        /// </summary>
        protected Vector2 scale;

        /// <summary>
        /// Camera scale- 1.0 is 100%, 2.0 is 200%, etc
        /// </summary>
        public Vector2 Scale
        {
            get { return scale; }
            set 
            { 
                scale = value;
                isTransformDirty = true;
            }
        }

        /// <summary>
        /// Amount of time allowed to transition between two modes
        /// </summary>
        protected float transitionTime;

        /// <summary>
        /// Amount of time remaining in the transition
        /// </summary>
        protected float transitionRemaining;

        /// <summary>
        /// The position of the camera during a transition
        /// </summary>
        protected Vector2 transitionPos;

        /// <summary>
        /// The position of the camera in world space in the last frame
        /// </summary>
        protected Vector2 lastFramePos;

        /// <summary>
        /// The position of the camera in world space in the last frame
        /// </summary>
        public Vector2 LastFramePosition
        {
            get { return lastFramePos; }
        }

        /// <summary>
        /// The position of the camera in world space in the current frame
        /// </summary>
        public Vector2 CurrentFramePosition
        {
            get
            {
                Vector2 cfPos;
                switch (mode)
                {
                    case CameraMode.LockedTarget:
                        cfPos = target.GetPosition();
                        break;
                    case CameraMode.LockedPos:
                        cfPos = targetPos;
                        break;
                    case CameraMode.TtTTransition:
                    case CameraMode.TtVTransition:
                    case CameraMode.VtTTransition:
                    case CameraMode.VtVTransition:
                        cfPos = transitionPos;
                        break;
                    default:
                        cfPos = Vector2.Zero;
                        break;
                }
                return cfPos;
            }
        }

        /// <summary>
        /// The change in position of the camera between this frame and last frame
        /// </summary>
        public Vector2 DeltaFramePosition
        {
            get { return CurrentFramePosition - LastFramePosition; }
        }

        private float dt;

        /// <summary>
        /// Returns the amount of time that elapsed in the last frame, as seen by the camera.
        /// </summary>
        /// <remarks>
        /// This is useful for effects that should be applied based on time and not frame cycle,
        /// Such as blur trails.
        /// </remarks>
        public float Dt
        {
            get { return dt; }
        }

        /// <summary>
        /// The camera effects currently applied to (or at least tracked by) this camera
        /// </summary>
        protected List<CameraEffect> effects;

        #endregion

        #region Initialization

        /// <summary>
        /// Create a camera with a given viewport and scale 1.
        /// </summary>
        /// <param name="viewport">The viewport this camera maps to</param>
        public Camera(Viewport viewport) : this(viewport, Vector2.One){ }

        /// <summary>
        /// Create a camera with a given viewport and a given scale.
        /// </summary>
        /// <param name="viewport">The viewport this camera maps to</param>
        /// <param name="scale">Scaling factor (0.5 is 50% zoom, 2.0 is 200% zoom)</param>
        public Camera(Viewport viewport, Vector2 scale)
        {
            this.dt = 0;
            this.scale = scale;
            mode = CameraMode.LockedPos;
            targetPos = Vector2.Zero;
            this.viewport = viewport;
            lastFramePos = targetPos;
            effects = new List<CameraEffect>();
            isTransformDirty = true;
        }

        /// <summary>
        /// Copy Constructor
        /// </summary>
        /// <param name="camera">The camera to copy</param>
        public Camera(Camera camera)
        {
            dt = camera.dt;

            mode = camera.mode; 
            
            oldTarget = camera.oldTarget;
            target = camera.target;
            oldTargetPos = camera.oldTargetPos; 
            targetPos = camera.targetPos;

            transitionPos = camera.transitionPos;
            transitionRemaining = camera.transitionRemaining;
            transitionTime = camera.transitionTime;

            lastFramePos = camera.lastFramePos;
            
            effects = new List<CameraEffect>(camera.effects);
            
            isTransformDirty = camera.isTransformDirty;
            transformMatrix = camera.transformMatrix;
        }

        #endregion

        #region Update

        /// <summary>
        /// Update camera transitions and effects
        /// </summary>
        /// <param name="elapsedTime"></param>
        public virtual void Update(float elapsedTime)
        {
            dt = elapsedTime;
            UpdateTransition(elapsedTime);
            UpdateEffects(elapsedTime);
            CalculateTransformMatrix();
        }

        /// <summary>
        /// Update any active transitions and move between lock states
        /// </summary>
        private void UpdateTransition(float elapsedTime)
        {
            // No Transition to update
            if (mode == CameraMode.LockedPos || mode == CameraMode.LockedTarget)
                return;

            transitionRemaining -= elapsedTime;
            
            if (transitionRemaining <= 0)
            {
                // End the transition
                switch (mode)
                {
                    case CameraMode.TtTTransition:
                    case CameraMode.VtTTransition:
                        //The new target is stored in target, simply change mode
                        mode = CameraMode.LockedTarget;
                        break;
                    case CameraMode.TtVTransition:
                    case CameraMode.VtVTransition:
                        //The new target is stored in targetPos, simply change mode
                        mode = CameraMode.LockedPos;
                        break;
                }
            }
            else
            {
                // Transition is still going, update the transitionPosition
                Vector2 initialPos, finalPos;
                switch (mode)
                {
                    case CameraMode.TtTTransition:
                        initialPos = oldTarget.GetPosition();
                        finalPos = target.GetPosition();
                        break;
                    case CameraMode.VtTTransition:
                        initialPos = oldTargetPos;
                        finalPos = target.GetPosition();
                        break;
                    case CameraMode.TtVTransition:
                        initialPos = oldTarget.GetPosition();
                        finalPos = targetPos;
                        break;
                    case CameraMode.VtVTransition:
                        initialPos = oldTargetPos;
                        finalPos = targetPos;
                        break;
                    default:
                        initialPos = finalPos = Vector2.Zero;
                        break;
                }


                float pctTransitionRemaining = transitionRemaining / transitionTime;
                float pctTransitionComplete = 1 - pctTransitionRemaining;
                float powerPctComplete = (float)Math.Pow(pctTransitionComplete, 0.5);

                Vector2 deltaPos = finalPos - initialPos;
                transitionPos = initialPos + powerPctComplete * deltaPos;

            }
        }

        /// <summary>
        /// Update camera effects, removing any that are inactive
        /// </summary>
        private void UpdateEffects(float elapsedTime)
        {
            List<CameraEffect> deadEffects = new List<CameraEffect>();
            foreach (var effect in effects)
            {
                if (effect.IsActive)
                    effect.Update(elapsedTime);
                if (!effect.IsActive)
                    deadEffects.Add(effect);
            }
            foreach (var effect in deadEffects)
            {
                // Give the effect a chance to clean up
                effect.End();
                effects.Remove(effect);
            }
        }

        /// <summary>
        /// Advances the frame, so that position delta can be known within a timeframe.
        /// This method should be called as late as possible; end of draw is a good place.
        /// </summary>
        public void AdvanceFrame()
        {
            //Update LastFramePosition
            lastFramePos = CurrentFramePosition;

        }

        #endregion

        #region Tracking Methods

        /// <summary>
        /// Lock the camera onto the specified position.
        /// </summary>
        /// <param name="pos">The position "lock on" to</param>
        /// <param name="immediate">If the lock is not immediate, the last transition position is saved and the transition finishes from transition position to the new position</param>
        public void LockPosition(Vector2 pos, bool immediate)
        {
            if (immediate)
            {
                // End the transition, set the pos as the current vector lock
                FinishTransition();
                mode = CameraMode.LockedPos;
                targetPos = pos;
            }
            else
            {
                switch (mode)
                {
                    case CameraMode.LockedPos:
                        oldTargetPos = targetPos;
                        break;
                    case CameraMode.LockedTarget:
                        oldTargetPos = target.GetPosition();
                        break;
                    case CameraMode.TtTTransition:
                    case CameraMode.TtVTransition:
                    case CameraMode.VtTTransition:
                    case CameraMode.VtVTransition:
                        oldTargetPos = transitionPos;
                        break;
                }

                //Complete the transition from its current position
                targetPos = pos;
                mode = CameraMode.VtVTransition;
            }
            isTransformDirty = true;

        }

        /// <summary>
        /// Lock the camera onto the specified target.
        /// </summary>
        /// <param name="target">The GameplayObject to follow or "lock on" to</param>
        /// <param name="immediate">If the lock is not immediate, the last transition position is saved and the transition finishes from transition position to the new target</param>
        public void LockTarget(IPositionable target, bool immediate)
        {
            if (immediate)
            {
                // End the transition, set the pos as the current vector lock
                FinishTransition();
                mode = CameraMode.LockedTarget;
                this.target = target;
            }
            else
            {
                switch (mode)
                {
                    case CameraMode.LockedPos:
                        oldTargetPos = targetPos;
                        break;
                    case CameraMode.LockedTarget:
                        oldTargetPos = target.GetPosition();
                        break;
                    case CameraMode.TtTTransition:
                    case CameraMode.TtVTransition:
                    case CameraMode.VtTTransition:
                    case CameraMode.VtVTransition:
                        oldTargetPos = transitionPos;
                        break;
                }
                //Complete the transition from its current position
                this.target = target;
                mode = CameraMode.VtTTransition;
            }
            isTransformDirty = true;
        }

        /// <summary>
        /// Very much like LockTarget(target, immediate) except it allows a specification of duration.
        /// </summary>
        /// <param name="target">The GameplayObject to follow or "lock on" to</param>
        /// <param name="duration">Amount of time for the transition from last target or pos, or transition position (depending on last mode)</param>
        /// <param name="immediate">If the lock is not immediate, the last transition position is saved and the transition finishes from transition position to the new target</param>
        public void TransitionTo(IPositionable target, float duration, bool immediate)
        {
            LockTarget(target, immediate);
            if (!immediate)
                transitionRemaining = transitionTime = duration;
        }

        /// <summary>
        /// Very much like LockPosition(pos, immediate) except it allows a specification of duration.
        /// </summary>
        /// <param name="pos">The position "lock on" to</param>
        /// <param name="duration">Amount of time for the transition from last target or pos, or transition position (depending on last mode)</param>
        /// <param name="immediate">If the lock is not immediate, the last transition position is saved and the transition finishes from transition position to the new position</param>
        public void TransitionTo(Vector2 pos, float duration, bool immediate)
        {
            LockPosition(pos, immediate);
            if (!immediate)
                transitionRemaining = transitionTime = duration;
        }

        /// <summary>
        /// Immediately finish any transition, and copy current target information
        /// to its equivalent 'old' field
        /// </summary>
        private void FinishTransition()
        {
            oldTarget = target;
            oldTargetPos = targetPos;
            transitionPos = Vector2.Zero;
            transitionRemaining = 0;
        }

        #endregion

        #region Effects

        /// <summary>
        /// Add an effect to this camera
        /// </summary>
        /// <param name="effect">The CameraEffect to add</param>
        public void AddEffect(CameraEffect effect)
        {
            effects.Add(effect);
            isTransformDirty = true;

        }

        /// <summary>
        /// Remove an effect from this camera
        /// </summary>
        /// <param name="effect">The effect to remove</param>
        public void RemoveEffect(CameraEffect effect)
        {
            effect.End();
            effects.Remove(effect);
            isTransformDirty = true;
        }

        /// <summary>
        /// Immediately end a camera effect, regardless of its remaining duration
        /// </summary>
        /// <param name="effect">The effect to end</param>
        public void EndEffect(CameraEffect effect)
        {
            effect.End();
            RemoveEffect(effect);
            isTransformDirty = true;
        }

        #endregion

        #region Transform Matrix and Coordinate Transforms

        /// <summary>
        /// Converts a given 2D screen coordinate into World coordinates
        /// </summary>
        /// <param name="screenCoords">The point (in screen coordinates) to convert to world coordinates</param>
        /// <returns></returns>
        public Vector2 Screen2WorldCoords(Vector2 screenCoords)
        {
            return new Vector2((screenCoords.X - TransformMatrix.Translation.X) / Scale.X,
                               (screenCoords.Y - TransformMatrix.Translation.Y) / Scale.X);
        }

        /// <summary>
        /// Returns the transform matrix from world -> screen.
        /// When rendering, use this transform with:
        /// batch.Begin(SpriteSortMode, BlendState, SamplerState, DepthStencilState, RasterizerState, Effect, camera.TransformMatrix);
        /// </summary>
        public Matrix TransformMatrix
        {
            get
            {
                if (isTransformDirty)
                    CalculateTransformMatrix();
                return transformMatrix;
            }
        }

        /// <summary>
        /// Calculate the Transform Matrix for the current frame
        /// </summary>
        private void CalculateTransformMatrix()
        {
            Matrix originMatrix, rotateMatrix, scaleMatrix, positionMatrix;
            float offsetX, offsetY, offsetRotation, offsetScaleX, offsetScaleY;
            CalculateEffectOffsets(out offsetX, out offsetY, out offsetRotation, out offsetScaleX, out offsetScaleY);

            #region Position Matrix

            Vector2 center = new Vector2(viewport.X, viewport.Y);
            switch (mode)
            {
                case CameraMode.LockedTarget:
                    center += target.GetPosition();
                    break;
                case CameraMode.LockedPos:
                    center += targetPos;
                    break;
                case CameraMode.TtTTransition:
                case CameraMode.TtVTransition:
                case CameraMode.VtTTransition:
                case CameraMode.VtVTransition:
                    center += transitionPos;
                    break;
            }

            center.X += offsetX;
            center.Y += offsetY;
            positionMatrix = Matrix.CreateTranslation(new Vector3(-center.X, -center.Y, 0f));

            #endregion

            // Rotation Matrix
            rotateMatrix = Matrix.CreateRotationZ(rotation + offsetRotation);

            // Scale Matrix
            Vector3 scaleVector = new Vector3(scale.X + offsetScaleX, scale.Y + offsetScaleY, 1);
            scaleMatrix = Matrix.CreateScale(scaleVector);

            // Origin Matrix
            Vector3 viewPortCenter = new Vector3(viewport.Width / 2, viewport.Height / 2, 0);
            originMatrix = Matrix.CreateTranslation(viewPortCenter);

            // Cache the result
            transformMatrix = positionMatrix * rotateMatrix * scaleMatrix * originMatrix;
            isTransformDirty = false;
        }

        /// <summary>
        /// Sum up the offsets from active effects
        /// </summary>
        private void CalculateEffectOffsets(out float offsetX, out float offsetY, out float offsetRot, out float offsetScaleX, out float offsetScaleY)
        {
            float[] offsets = { 0, 0, 0, 0, 0};
            float[] effectOffsets = new float[5];

            foreach (var effect in effects)
            {
                if (!effect.IsActive)
                    continue;
                effectOffsets = effect.Offsets;
                for (int i = 0; i < 5; i++)
                    offsets[i] += effectOffsets[i];
            }

            offsetX = offsets[0];
            offsetY = offsets[1];
            offsetRot = offsets[2];
            offsetScaleX = offsets[3];
            offsetScaleY = offsets[4];
        }

        #endregion
    }
}
