#region Using Statements

using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

#endregion

namespace Engine
{
    #region enums

    /// <summary>
    /// Used to specify the left or right mouse button
    /// </summary>
    public enum MouseButton { None, Left, Right, Middle }

    /// <summary>
    /// Used to specify the left or right trigger
    /// </summary>
    public enum Trigger { None, Left, Right }

    /// <summary>
    /// Used to specify the left or right thumbstick
    /// </summary>
    public enum Thumbstick { None, Left, Right }

    /// <summary>
    /// Used to specify the direction of a thumbstick "press" (=1)
    /// </summary>
    public enum ThumbstickDirection { None, Up, Down, Left, Right }

    /// <summary>
    /// Specifies the type of binding an InputBinding has.
    /// </summary>
    /// <remarks>
    /// This allows high-level checks for the action associated with the Input,
    /// instead of worrying about the details of that input's type
    /// 
    /// Thumbstick is registered on ANY thumbstick activity with magnitude greater than ThumbstickThreshold
    /// ThumbstickDirection is registered when the specified thumbstick is moved past Threshold in that direction
    /// </remarks>
    public enum BindingType { None, Key, Button, Trigger, Thumbstick, ThumbstickDirection, MouseButton }

    /// <summary>
    /// Which frame (previous or current) you are querying.
    /// Used when asking about a key's state
    /// </summary>
    public enum FrameState { Previous, Current }

    #endregion

    public class InputBinding
    {
        #region Fields

        protected BindingType type;
        public BindingType BindingType
        {
            get { return type; }
        }

        public ThumbstickDirection ThumbstickDirection;
        public MouseButton MouseButton;
        public Thumbstick Thumbstick;
        public Trigger Trigger;
        public Buttons Button;
        public Keys Key;

        public Modifier[] Modifiers;

        #endregion

        #region Initialiation

        public InputBinding() : this(BindingType.None) { }
        public InputBinding(BindingType type, params Modifier[] modifiers)
        {
            this.type = type;
            ThumbstickDirection = ThumbstickDirection.None;
            MouseButton = MouseButton.None;
            Thumbstick = Thumbstick.None;
            Trigger = Trigger.None;
            Key = Keys.None;
            Button = Buttons.BigButton;

            SetModifiers(modifiers);
        }

        #endregion

        #region SetBindings

        public void SetBinding(ThumbstickDirection thumbstickDirection, Thumbstick thumbstick, params Modifier[] modifiers)
        {
            ClearBindings();
            this.ThumbstickDirection = thumbstickDirection;
            this.Thumbstick = thumbstick;
            this.type = BindingType.ThumbstickDirection;
            SetModifiers(modifiers);
        }
        public void SetBinding(MouseButton mouseButton, params Modifier[] modifiers)
        {
            ClearBindings();
            this.MouseButton = mouseButton;
            this.type = BindingType.MouseButton;
            SetModifiers(modifiers);
        }
        public void SetBinding(Thumbstick thumbstick, params Modifier[] modifiers)
        {
            ClearBindings();
            this.Thumbstick = thumbstick;
            this.type = BindingType.Thumbstick;
            SetModifiers(modifiers);
        }
        public void SetBinding(Trigger trigger, params Modifier[] modifiers)
        {
            ClearBindings();
            this.Trigger = trigger;
            this.type = BindingType.Trigger;
            SetModifiers(modifiers);
        }
        public void SetBinding(Buttons button, params Modifier[] modifiers)
        {
            ClearBindings();
            this.Button = button;
            this.type = BindingType.Button;
            SetModifiers(modifiers);
        }
        public void SetBinding(Keys key, params Modifier[] modifiers)
        {
            ClearBindings();
            this.Key = key;
            this.type = BindingType.Key;
            SetModifiers(modifiers);
        }

        #endregion

        private void ClearBindings()
        {
            type = BindingType.None;
        }
        private void SetModifiers(params Modifier[] modifiers)
        {
            Modifiers = new Modifier[modifiers.Length];
            Array.Copy(modifiers, Modifiers, modifiers.Length);
        }

        public bool ModifiersMatch(KeyboardState keyState)
        {   
            return (Modifier.Alt.IsActive(keyState) == Modifiers.Contains(Modifier.Alt) &&
                    Modifier.Ctrl.IsActive(keyState) == Modifiers.Contains(Modifier.Ctrl) &&
                    Modifier.Shift.IsActive(keyState) == Modifiers.Contains(Modifier.Shift));   
        }
    }

    public class Modifier
    {
        Keys key1, key2;
        private Modifier(Keys key1, Keys key2)
        {
            this.key1 = key1;
            this.key2 = key2;
        }

        public bool IsActive(KeyboardState keyboardState)
        {
            return keyboardState.IsKeyDown(key1) || keyboardState.IsKeyDown(key2);
        }

        private static Modifier ctrl;
        public static Modifier Ctrl
        {
            get
            {
                if (ctrl == null)
                    ctrl = new Modifier(Keys.LeftControl, Keys.RightControl);
                return ctrl;
            }
        }
        
        private static Modifier alt;
        public static Modifier Alt
        {
            get
            {
                if (alt == null)
                    alt = new Modifier(Keys.LeftAlt, Keys.RightAlt);
                return alt;
            }
        }
        
        private static Modifier shift;
        public static Modifier Shift
        {
            get
            {
                if (shift == null)
                    shift = new Modifier(Keys.LeftShift, Keys.RightShift);
                return shift;
            }
        }

    }

    public class Input
    {
        #region Fields

        protected KeyboardState LastKeyboardState; 
        protected KeyboardState CurrentKeyboardState;

        protected GamePadState LastGamePadState;
        protected GamePadState CurrentGamePadState;

        protected MouseState LastMouseState;
        protected MouseState CurrentMouseState;

        protected Dictionary<String, InputBinding> keybindings;

        protected float thumbstickThreshold = 0.0f;
        public float ThumbstickThreshold
        {
            get { return thumbstickThreshold; }
            set { thumbstickThreshold = value; }
        }

        protected float triggerThreshold = 0.0f;
        public float TriggerThreshold
        {
            get { return triggerThreshold; }
            set { triggerThreshold = value; }
        }

        #endregion

        #region Initialization

        public Input()
        {
            keybindings = new Dictionary<string, InputBinding>();
        }
        public Input(Input input)
        {
            LastKeyboardState = input.LastKeyboardState;
            CurrentKeyboardState = input.CurrentKeyboardState;

            LastGamePadState = input.LastGamePadState;
            CurrentGamePadState = input.CurrentGamePadState;

            LastMouseState = input.LastMouseState;
            CurrentMouseState = input.CurrentMouseState;

            triggerThreshold = input.triggerThreshold;
            thumbstickThreshold = input.thumbstickThreshold;

            keybindings = new Dictionary<string, InputBinding>(input.keybindings);

        }

        #endregion

        /// <summary>
        /// Reads the latest state of the keyboard and gamepad.
        /// </summary>
        /// <remarks>
        /// This should be called at the beginning of your update loop, so that game logic
        /// uses latest values.
        /// Calling update at the end of update loop will have those keys processed
        /// in the next frame.
        /// </remarks>
        public void Update()
        {
            LastKeyboardState = CurrentKeyboardState;
            LastGamePadState = CurrentGamePadState;
            LastMouseState = CurrentMouseState;

            CurrentKeyboardState = Keyboard.GetState();
            CurrentGamePadState = GamePad.GetState(PlayerIndex.One);
            CurrentMouseState = Mouse.GetState();
        }

        #region AddKeyBinding Methods

        public void AddKeyBinding(string bindingName, InputBinding inputBinding)
        {
            // Make sure there isn't already a biding with that name
            RemoveKeyBinding(bindingName);
            keybindings.Add(bindingName, inputBinding);
        }
        public void AddKeyBinding(string bindingName, ThumbstickDirection thumbstickDirection, Thumbstick thumbstick, params Modifier[] modifiers)
        {
            InputBinding inputBinding = new InputBinding();
            inputBinding.SetBinding(thumbstickDirection, thumbstick, modifiers);
            AddKeyBinding(bindingName, inputBinding);
        }
        public void AddKeyBinding(string bindingName, MouseButton mouseButton, params Modifier[] modifiers)
        {
            InputBinding inputBinding = new InputBinding();
            inputBinding.SetBinding(mouseButton, modifiers);
            AddKeyBinding(bindingName, inputBinding);
        }
        public void AddKeyBinding(string bindingName, Thumbstick thumbstick, params Modifier[] modifiers)
        {
            InputBinding inputBinding = new InputBinding();
            inputBinding.SetBinding(thumbstick, modifiers);
            AddKeyBinding(bindingName, inputBinding);
        }
        public void AddKeyBinding(string bindingName, Trigger trigger, params Modifier[] modifiers)
        {
            InputBinding inputBinding = new InputBinding();
            inputBinding.SetBinding(trigger, modifiers);
            AddKeyBinding(bindingName, inputBinding);
        }
        public void AddKeyBinding(string bindingName, Buttons button, params Modifier[] modifiers)
        {
            InputBinding inputBinding = new InputBinding();
            inputBinding.SetBinding(button, modifiers);
            AddKeyBinding(bindingName, inputBinding);
        }
        public void AddKeyBinding(string bindingName, Keys key, params Modifier[] modifiers)
        {
            InputBinding inputBinding = new InputBinding();
            inputBinding.SetBinding(key, modifiers);
            AddKeyBinding(bindingName, inputBinding);
        }

        #endregion

        /// <summary>
        /// Removes the binding associated with the specified key
        /// </summary>
        /// <param name="key">The name of the keybinding to remove</param>
        public void RemoveKeyBinding(string key)
        {
            if(HasKeyBinding(key))
                keybindings.Remove(key);
        }

        /// <summary>
        /// Update the binding associated with the specified key,
        /// overwriting that binding with the newBinding
        /// </summary>
        /// <param name="key">The name of the keybinding to update</param>
        /// <param name="newBinding">The new information to associate with the key</param>
        public void UpdateKeyBinding(string key, InputBinding newBinding)
        {
            RemoveKeyBinding(key);
            AddKeyBinding(key, newBinding);
        }

        /// <summary>
        /// Returns true if the input has a binding associated with a key
        /// </summary>
        /// <param name="key">The name of the keybinding to check for</param>
        /// <returns></returns>
        public bool HasKeyBinding(string key)
        {
            return keybindings.ContainsKey(key);
        }

        /// <summary>
        /// Checks the CURRENT frame
        /// Returns if the keybinding associated with the string key is active.
        /// Active can mean pressed for buttons, or above threshold for thumbsticks/triggers
        /// </summary>
        /// <param name="key">The string that the keybinding was stored under</param>
        public bool IsKeyBindingActive(string key)
        {
            return IsKeyBindingActive(key, FrameState.Current);
        }

        /// <summary>
        /// Returns if the keybinding associated with the string key is active in the specified frame.
        /// Active can mean pressed for buttons, or above threshold for thumbsticks/triggers
        /// </summary>
        /// <param name="key">The string that the keybinding was stored under</param>
        /// <param name="state">The frame to inspect for the press- the current frame or the previous frame</param>
        /// <returns></returns>
        public bool IsKeyBindingActive(string key, FrameState state)
        {
            bool isActive = false;
            if (HasKeyBinding(key))
            {
                InputBinding binding = keybindings[key];
                switch (binding.BindingType)
                {
                    case BindingType.ThumbstickDirection:
                        isActive = IsThumbstickDirectionActive(binding.ThumbstickDirection, binding.Thumbstick, state);
                        break;
                    case BindingType.MouseButton:
                        isActive = IsMouseButtonActive(binding.MouseButton, state);
                        break;
                    case BindingType.Thumbstick:
                        isActive = IsThumbstickActive(binding.Thumbstick, state);
                        break;
                    case BindingType.Button:
                        isActive = IsButtonActive(binding.Button, state);
                        break;
                    case BindingType.Key:
                        isActive = IsKeyActive(binding.Key, state);
                        break;
                    case BindingType.Trigger:
                        isActive = IsTriggerActive(binding.Trigger, state);
                        break;
                    case BindingType.None:
                    default:
                        break;
                }
                KeyboardState keyState = state == FrameState.Current ? CurrentKeyboardState : LastKeyboardState;
                isActive &= binding.ModifiersMatch(keyState);

            }
            return isActive;
        }

        /// <summary>
        /// Returns if the keybinding associated with the string key was pressed this frame,
        /// but not last.  To register on key up, use IsKeyBindingNewRelease
        /// </summary>
        /// <param name="key">The string that the keybinding was stored under</param>
        /// <returns></returns>
        public bool IsKeyBindingPress(string key)
        {
            return IsKeyBindingActive(key, FrameState.Current) && !IsKeyBindingActive(key, FrameState.Previous);
        }

        /// <summary>
        /// Returns if the keybinding associated with the string key was pressed last frame,
        /// but not this frame (s.t. it was released in this frame).  
        /// To register on key down, use IsKeyBindingNewPress
        /// </summary>
        /// <param name="key">The string that the keybinding was stored under</param>
        /// <returns></returns>
        public bool IsKeyBindingRelease(string key)
        {
            return IsKeyBindingActive(key, FrameState.Previous) && !IsKeyBindingActive(key, FrameState.Current);
        }

        #region IsKeyBindingActive Helpers

        private bool IsKeyActive(Keys key, FrameState state)
        {
            KeyboardState keyState = state == FrameState.Current ? CurrentKeyboardState : LastKeyboardState;
            return keyState.IsKeyDown(key);
        }
        private bool IsButtonActive(Buttons button, FrameState state)
        {
            GamePadState gamepadState = state == FrameState.Current ? CurrentGamePadState : LastGamePadState;
            return gamepadState.IsButtonDown(button);
        }
        private bool IsMouseButtonActive(MouseButton button, FrameState state)
        {
            MouseState mouseState = state == FrameState.Current ? CurrentMouseState : LastMouseState;
            ButtonState buttonState;
            switch(button)
            {
                case MouseButton.Left:
                    buttonState = mouseState.LeftButton;
                    break;
                case MouseButton.Right:
                    buttonState = mouseState.RightButton;
                    break;
                case MouseButton.Middle:
                    buttonState = mouseState.MiddleButton;
                    break;
                default:
                    buttonState = ButtonState.Released;
                    break;
            }

            return buttonState == ButtonState.Pressed;
        }
        private bool IsThumbstickDirectionActive(ThumbstickDirection direction, Thumbstick thumbstick, FrameState state)
        {
            GamePadState gamepadState = state == FrameState.Current ? CurrentGamePadState : LastGamePadState;
            Vector2 gamepadThumbstick = thumbstick == Thumbstick.Left ? gamepadState.ThumbSticks.Left : gamepadState.ThumbSticks.Right;
            bool isActive = false;
            switch (direction)
            {
                case ThumbstickDirection.Up:
                    isActive = (gamepadThumbstick.Y >= thumbstickThreshold);
                    break;
                case ThumbstickDirection.Down:
                    isActive = (gamepadThumbstick.Y <= -thumbstickThreshold);
                    break;
                case ThumbstickDirection.Left:
                    isActive = (gamepadThumbstick.X <= -thumbstickThreshold);
                    break;
                case ThumbstickDirection.Right:
                    isActive = (gamepadThumbstick.X >= thumbstickThreshold);
                    break;
                default:
                    break;
            }
            
            return isActive;
        }
        private bool IsThumbstickActive(Thumbstick thumbstick, FrameState state)
        {
            GamePadState gamepadState = state == FrameState.Current ? CurrentGamePadState : LastGamePadState;
            Vector2 gamepadThumbstick = thumbstick == Thumbstick.Left ? gamepadState.ThumbSticks.Left : gamepadState.ThumbSticks.Right;
            return gamepadThumbstick.Length() >= thumbstickThreshold;
        }
        private bool IsTriggerActive(Trigger trigger, FrameState state)
        {
            GamePadState gamepadState = state == FrameState.Current ? CurrentGamePadState : LastGamePadState;
            float triggerMag = trigger == Trigger.Left ? gamepadState.Triggers.Left : gamepadState.Triggers.Right;
            return triggerMag >= triggerThreshold;
        }

        #endregion

        /// <summary>
        /// Get the position of the mouse in the current frame.
        /// </summary>
        /// <returns></returns>
        public Vector2 GetMousePos()
        {
            return GetMousePos(FrameState.Current);
        }

        /// <summary>
        /// Get the position of the mouse in the specified state.
        /// </summary>
        /// <param name="state">The frame to inspect for the position- the current frame or the previous frame</param>
        /// <returns></returns>
        public Vector2 GetMousePos(FrameState state)
        {
            MouseState mouseState = state == FrameState.Current ? CurrentMouseState : LastMouseState;
            return new Vector2(mouseState.X, mouseState.Y);
        }
    }
}
