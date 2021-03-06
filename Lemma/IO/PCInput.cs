﻿using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Lemma.Components
{
	public class PCInput : ComponentBind.Component<Main>, IUpdateableComponent
	{
		public enum MouseButton { None, LeftMouseButton, MiddleMouseButton, RightMouseButton }

		public enum InputState { Down, Up }

		public struct PCInputBinding
		{
			[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
			public Keys Key;
			[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
			public MouseButton MouseButton;
			[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
			public Buttons GamePadButton;

			public override string ToString()
			{
				string value = null;
				if (this.Key != Keys.None)
					value = "\\" + this.Key.ToString();
				else if (this.MouseButton != PCInput.MouseButton.None)
					value = "\\" + this.MouseButton.ToString();

				if (GamePad.GetState(PlayerIndex.One).IsConnected && this.GamePadButton != 0)
					return "\\" + this.GamePadButton.ToString();
				else if (value != null)
					return value;
				else
					return "[?]";
			}
		}

		public Property<Vector2> Mouse = new Property<Vector2> { };
		public Property<int> ScrollWheel = new Property<int> { };
		public Property<bool> LeftMouseButton = new Property<bool> { };
		public Property<bool> MiddleMouseButton = new Property<bool> { };
		public Property<bool> RightMouseButton = new Property<bool> { };

		public Command LeftMouseButtonDown = new Command();
		public Command MiddleMouseButtonDown = new Command();
		public Command RightMouseButtonDown = new Command();
		public Command LeftMouseButtonUp = new Command();
		public Command MiddleMouseButtonUp = new Command();
		public Command RightMouseButtonUp = new Command();
		public Command<int> MouseScrolled = new Command<int>();

		public Command<PCInputBinding> AnyInputDown = new Command<PCInputBinding>();

		public Property<bool> EnableMouse = new Property<bool> { Value = true };

		public void Bind(Property<PCInput.PCInputBinding> inputBinding, InputState state, Action action)
		{
			CommandBinding commandBinding = null;
			CommandBinding buttonBinding = null;
			Action rebindCommand = delegate()
			{
				if (commandBinding != null)
					this.Remove(commandBinding);

				if (buttonBinding != null)
					this.Remove(buttonBinding);

				PCInput.PCInputBinding ib = inputBinding;
				if (ib.Key == Keys.None && ib.MouseButton == PCInput.MouseButton.None)
					commandBinding = null;
				else
				{
					commandBinding = new CommandBinding(state == InputState.Up ? this.GetInputUp(ib) : this.GetInputDown(ib), action);
					this.Add(commandBinding);
				}

				if (ib.GamePadButton == 0)
					buttonBinding = null;
				else
				{
					buttonBinding = new CommandBinding(state == InputState.Up ? this.GetButtonUp(ib.GamePadButton) : this.GetButtonDown(ib.GamePadButton), action);
					this.Add(buttonBinding);
				}
			};
			this.Add(new NotifyBinding(rebindCommand, inputBinding));
			rebindCommand();
		}

		public void Bind(Property<bool> destination, Property<PCInput.PCInputBinding> inputBinding)
		{
			Binding<bool> binding = null;
			Binding<bool> buttonBinding = null;
			Action rebind = delegate()
			{
				if (binding != null)
					this.Remove(binding);
				
				if (buttonBinding != null)
					this.Remove(buttonBinding);

				PCInput.PCInputBinding ib = inputBinding;
				if (ib.Key == Keys.None && ib.MouseButton == PCInput.MouseButton.None)
					binding = null;
				else
				{
					switch (ib.MouseButton)
					{
						case MouseButton.None:
							binding = new Binding<bool>(destination, this.GetKey(ib.Key));
							break;
						case MouseButton.LeftMouseButton:
							binding = new Binding<bool>(destination, this.LeftMouseButton);
							break;
						case MouseButton.RightMouseButton:
							binding = new Binding<bool>(destination, this.RightMouseButton);
							break;
						case MouseButton.MiddleMouseButton:
							binding = new Binding<bool>(destination, this.MiddleMouseButton);
							break;
					}
					this.Add(binding);
				}

				if (ib.GamePadButton == 0)
					buttonBinding = null;
				else
				{
					buttonBinding = new Binding<bool>(destination, this.GetButton(ib.GamePadButton));
					this.Add(buttonBinding);
				}
			};
			this.Add(new NotifyBinding(rebind, inputBinding));
			rebind();
		}

		protected Dictionary<Keys, Property<bool>> keyProperties = new Dictionary<Keys, Property<bool>>();
		protected List<Keys> keys = new List<Keys>();

		protected Dictionary<Keys, Command> keyUpCommands = new Dictionary<Keys, Command>();
		protected Dictionary<Keys, Command> keyDownCommands = new Dictionary<Keys, Command>();

		protected Dictionary<Buttons, Property<bool>> buttonProperties = new Dictionary<Buttons, Property<bool>>();
		protected List<Buttons> buttons = new List<Buttons>();

		protected Dictionary<Buttons, Command> buttonUpCommands = new Dictionary<Buttons, Command>();
		protected Dictionary<Buttons, Command> buttonDownCommands = new Dictionary<Buttons, Command>();

		protected List<Action<PCInput.PCInputBinding>> nextInputListeners = new List<Action<PCInputBinding>>();

		public struct Chord
		{
			public Keys Modifier, Key;
			public MouseButton Mouse;
			
			public bool Exists
			{
				get
				{
					return this.Key != Keys.None || this.Mouse != MouseButton.None;
				}
			}

			public Chord(Keys key)
			{
				this.Key = key;
				this.Modifier = Keys.None;
				this.Mouse = MouseButton.None;
			}

			public Chord(Keys key, Keys modifier)
			{
				this.Key = key;
				this.Modifier = modifier;
				this.Mouse = MouseButton.None;
			}

			public Chord(MouseButton mouse)
			{
				this.Key = Keys.None;
				this.Modifier = Keys.None;
				this.Mouse = mouse;
			}

			public override string ToString()
			{
				if (this.Mouse != MouseButton.None)
				{
					switch (this.Mouse)
					{
						case MouseButton.RightMouseButton:
							return "Right-click";
						case MouseButton.MiddleMouseButton:
							return "Middle-click";
						default:
							return "Left-click";
					}
				}
				else
				{
					string key;
					switch (this.Key)
					{
						case Keys.OemPeriod:
							key = ".";
							break;
						case Keys.OemComma:
							key = ",";
							break;
						case Keys.D0:
							key = "0";
							break;
						case Keys.D1:
							key = "1";
							break;
						case Keys.D2:
							key = "2";
							break;
						case Keys.D3:
							key = "3";
							break;
						case Keys.D4:
							key = "4";
							break;
						case Keys.D5:
							key = "5";
							break;
						case Keys.D6:
							key = "6";
							break;
						case Keys.D7:
							key = "7";
							break;
						case Keys.D8:
							key = "8";
							break;
						case Keys.D9:
							key = "9";
							break;
						default:
							key = this.Key.ToString(); // That's all I feel like doing for now
							break;
					}
					if (this.Modifier == Keys.None)
						return key;
					else
					{
						string modifier;
						switch (this.Modifier)
						{
							case Keys.LeftControl:
								modifier = "Ctrl";
								break;
							case Keys.LeftAlt:
								modifier = "Alt";
								break;
							case Keys.LeftShift:
								modifier = "Shift";
								break;
							case Keys.LeftWindows:
								modifier = "Windows";
								break;
							default:
								modifier = this.Modifier.ToString();
								break;
						}
						return string.Format("{0}+{1}", modifier, key);
					}
				}
			}
		}

		protected Dictionary<Chord, Command> chords = new Dictionary<Chord, Command>();

		protected bool chordActivated = false;

		public PCInput()
		{
			this.Serialize = false;
		}

		public override Entity Entity
		{
			get
			{
				return base.Entity;
			}
			set
			{
				base.Entity = value;
				this.EnabledWhenPaused = false;
			}
		}

		private bool preventKeyDownEvents = false;

		public override void Awake()
		{
			base.Awake();
			this.Add(new CommandBinding(this.Disable, delegate()
			{
				// Release all the keys
				for (int i = 0; i < this.keys.Count; i++)
				{
					Keys key = this.keys[i];
					Property<bool> prop = this.keyProperties[key];
					if (prop)
					{
						Command command;
						if (this.keyUpCommands.TryGetValue(key, out command))
							command.Execute();
						prop.Value = false;
					}
				}

				// Release all the buttons
				for (int i = 0; i < this.buttons.Count; i++)
				{
					Buttons button = this.buttons[i];
					Property<bool> prop = this.buttonProperties[button];
					if (prop)
					{
						Command command;
						if (this.buttonUpCommands.TryGetValue(button, out command))
							command.Execute();
						prop.Value = false;
					}
				}

				this.chordActivated = false;

				// Release mouse buttons
				if (this.LeftMouseButton)
				{
					this.LeftMouseButton.Value = false;
					this.LeftMouseButtonUp.Execute();
				}

				if (this.RightMouseButton)
				{
					this.RightMouseButton.Value = false;
					this.RightMouseButtonUp.Execute();
				}

				if (this.MiddleMouseButton)
				{
					this.MiddleMouseButton.Value = false;
					this.MiddleMouseButtonUp.Execute();
				}
			}));

			this.Add(new CommandBinding(this.Enable, delegate()
			{
				// Don't send key-down events for the first frame after we're enabled.
				this.preventKeyDownEvents = true;
			}));
		}

		public Property<bool> GetKey(Keys key)
		{
			Property<bool> result;
			if (!this.keyProperties.TryGetValue(key, out result))
			{
				result = new Property<bool>();
				this.keyProperties.Add(key, result);
				this.keys.Add(key);
			}
			return result;
		}

		public Property<bool> GetButton(Buttons button)
		{
			if (button == 0)
				return new Property<bool>();

			Property<bool> result;
			if (!this.buttonProperties.TryGetValue(button, out result))
			{
				result = new Property<bool>();
				this.buttonProperties.Add(button, result);
				this.buttons.Add(button);
			}
			return result;
		}

		public Command GetKeyDown(Keys key)
		{
			Command result;
			if (!this.keyDownCommands.TryGetValue(key, out result))
			{
				this.GetKey(key);
				result = new Command();
				this.keyDownCommands.Add(key, result);
			}
			return result;
		}

		public Command GetKeyUp(Keys key)
		{
			Command result;
			if (!this.keyUpCommands.TryGetValue(key, out result))
			{
				this.GetKey(key);
				result = new Command();
				this.keyUpCommands.Add(key, result);
			}
			return result;
		}

		public Command GetButtonDown(Buttons button)
		{
			Command result;
			if (!this.buttonDownCommands.TryGetValue(button, out result))
			{
				this.GetButton(button);
				result = new Command();
				this.buttonDownCommands.Add(button, result);
			}
			return result;
		}

		public Command GetButtonUp(Buttons button)
		{
			Command result;
			if (!this.buttonUpCommands.TryGetValue(button, out result))
			{
				this.GetButton(button);
				result = new Command();
				this.buttonUpCommands.Add(button, result);
			}
			return result;
		}

		public void GetNextInput(Action<PCInput.PCInputBinding> listener)
		{
			this.nextInputListeners.Add(listener);
		}

		public bool GetInput(PCInputBinding binding)
		{
			bool result = false;

			if (binding.Key != Keys.None)
				result |= this.GetKey(binding.Key);

			switch (binding.MouseButton)
			{
				case MouseButton.LeftMouseButton:
					result |= this.LeftMouseButton;
					break;
				case MouseButton.MiddleMouseButton:
					result |= this.MiddleMouseButton;
					break;
				case MouseButton.RightMouseButton:
					result |= this.RightMouseButton;
					break;
				default:
					break;
			}

			if (binding.GamePadButton != 0)
				result |= this.GetButton(binding.GamePadButton);

			return result;
		}

		public Command GetInputUp(PCInputBinding binding)
		{
			if (binding.Key != Keys.None)
				return this.GetKeyUp(binding.Key);
			else if (binding.MouseButton != MouseButton.None)
			{
				switch (binding.MouseButton)
				{
					case MouseButton.LeftMouseButton:
						return this.LeftMouseButtonUp;
					case MouseButton.MiddleMouseButton:
						return this.MiddleMouseButtonUp;
					case MouseButton.RightMouseButton:
						return this.RightMouseButtonUp;
					default:
						return null;
				}
			}
			else
				return this.GetButtonUp(binding.GamePadButton);
		}

		public Command GetInputDown(PCInputBinding binding)
		{
			if (binding.Key != Keys.None)
				return this.GetKeyDown(binding.Key);
			else if (binding.MouseButton != MouseButton.None)
			{
				switch (binding.MouseButton)
				{
					case MouseButton.LeftMouseButton:
						return this.LeftMouseButtonDown;
					case MouseButton.MiddleMouseButton:
						return this.MiddleMouseButtonDown;
					case MouseButton.RightMouseButton:
						return this.RightMouseButtonDown;
					default:
						return null;
				}
			}
			else
				return this.GetButtonDown(binding.GamePadButton);
		}

		public Command GetChord(Chord chord)
		{
			Command output;
			if (!this.chords.TryGetValue(chord, out output))
			{
				Command cmd = new Command();
				this.chords.Add(chord, cmd);
				output = cmd;
			}
			return output;
		}

		private void notifyNextInputListeners(PCInput.PCInputBinding input)
		{
			for (int i = 0; i < this.nextInputListeners.Count; i++)
				this.nextInputListeners[i](input);
			this.nextInputListeners.Clear();
			this.preventKeyDownEvents = true;
		}

		private bool temporarilyIgnoreLMB;

		public virtual void Update(float elapsedTime)
		{
			if (!this.main.IsActive)
				return;

			if (!this.main.LastActive)
			{
				if (this.main.MouseState.Value.LeftButton == ButtonState.Pressed)
					this.temporarilyIgnoreLMB = true;
			}

			KeyboardState keyboard = this.main.KeyboardState;

			Keys[] keys = keyboard.GetPressedKeys();
			if (keys.Length > 0 && this.nextInputListeners.Count > 0)
				this.notifyNextInputListeners(new PCInputBinding { Key = keys[0] });

			if (this.AnyInputDown.Bindings.Count > 0)
			{
				Keys[] lastKeys = this.main.LastKeyboardState.Value.GetPressedKeys();
				for (int i = 0; i < keys.Length; i++)
				{
					if (!lastKeys.Contains(keys[i]))
						this.AnyInputDown.Execute(new PCInputBinding { Key = keys[i] });
				}
			}

			for (int i = 0; i < this.keys.Count; i++)
			{
				Keys key = this.keys[i];
				Property<bool> prop = this.keyProperties[key];
				bool newValue = keyboard.IsKeyDown(key);
				if (prop != newValue)
				{
					prop.Value = newValue;
					if (!this.preventKeyDownEvents)
					{
						if (newValue)
						{
							Command command;
							if (this.keyDownCommands.TryGetValue(key, out command))
								command.Execute();
						}
						else
						{
							Command command;
							if (this.keyUpCommands.TryGetValue(key, out command))
								command.Execute();
						}
					}
				}
			}

			GamePadState gamePad = this.main.GamePadState;
			if (gamePad.IsConnected)
			{
				if (this.nextInputListeners.Count > 0 || this.AnyInputDown.Bindings.Count > 0)
				{
					GamePadState lastGamePad = this.main.LastGamePadState;
					List<Buttons> buttons = new List<Buttons>();
					if (gamePad.IsButtonDown(Buttons.A) && !lastGamePad.IsButtonDown(Buttons.A))
						buttons.Add(Buttons.A);
					if (gamePad.IsButtonDown(Buttons.B) && !lastGamePad.IsButtonDown(Buttons.B))
						buttons.Add(Buttons.B);
					if (gamePad.IsButtonDown(Buttons.Back) && !lastGamePad.IsButtonDown(Buttons.Back))
						buttons.Add(Buttons.Back);
					if (gamePad.IsButtonDown(Buttons.DPadDown) && !lastGamePad.IsButtonDown(Buttons.DPadDown))
						buttons.Add(Buttons.DPadDown);
					if (gamePad.IsButtonDown(Buttons.DPadLeft) && !lastGamePad.IsButtonDown(Buttons.DPadLeft))
						buttons.Add(Buttons.DPadLeft);
					if (gamePad.IsButtonDown(Buttons.DPadRight) && !lastGamePad.IsButtonDown(Buttons.DPadRight))
						buttons.Add(Buttons.DPadRight);
					if (gamePad.IsButtonDown(Buttons.DPadUp) && !lastGamePad.IsButtonDown(Buttons.DPadUp))
						buttons.Add(Buttons.DPadUp);
					if (gamePad.IsButtonDown(Buttons.LeftShoulder) && !lastGamePad.IsButtonDown(Buttons.LeftShoulder))
						buttons.Add(Buttons.LeftShoulder);
					if (gamePad.IsButtonDown(Buttons.RightShoulder) && !lastGamePad.IsButtonDown(Buttons.RightShoulder))
						buttons.Add(Buttons.RightShoulder);
					if (gamePad.IsButtonDown(Buttons.LeftStick) && !lastGamePad.IsButtonDown(Buttons.LeftStick))
						buttons.Add(Buttons.LeftStick);
					if (gamePad.IsButtonDown(Buttons.RightStick) && !lastGamePad.IsButtonDown(Buttons.RightStick))
						buttons.Add(Buttons.RightStick);
					if (gamePad.IsButtonDown(Buttons.LeftThumbstickDown) && !lastGamePad.IsButtonDown(Buttons.LeftThumbstickDown))
						buttons.Add(Buttons.LeftThumbstickDown);
					if (gamePad.IsButtonDown(Buttons.LeftThumbstickRight) && !lastGamePad.IsButtonDown(Buttons.LeftThumbstickRight))
						buttons.Add(Buttons.LeftThumbstickRight);
					if (gamePad.IsButtonDown(Buttons.LeftThumbstickLeft) && !lastGamePad.IsButtonDown(Buttons.LeftThumbstickLeft))
						buttons.Add(Buttons.LeftThumbstickLeft);
					if (gamePad.IsButtonDown(Buttons.LeftThumbstickUp) && !lastGamePad.IsButtonDown(Buttons.LeftThumbstickUp))
						buttons.Add(Buttons.LeftThumbstickUp);
					if (gamePad.IsButtonDown(Buttons.RightThumbstickDown) && !lastGamePad.IsButtonDown(Buttons.RightThumbstickDown))
						buttons.Add(Buttons.RightThumbstickDown);
					if (gamePad.IsButtonDown(Buttons.RightThumbstickRight) && !lastGamePad.IsButtonDown(Buttons.RightThumbstickRight))
						buttons.Add(Buttons.RightThumbstickRight);
					if (gamePad.IsButtonDown(Buttons.RightThumbstickLeft) && !lastGamePad.IsButtonDown(Buttons.RightThumbstickLeft))
						buttons.Add(Buttons.RightThumbstickLeft);
					if (gamePad.IsButtonDown(Buttons.RightThumbstickUp) && !lastGamePad.IsButtonDown(Buttons.RightThumbstickUp))
						buttons.Add(Buttons.RightThumbstickUp);
					if (gamePad.IsButtonDown(Buttons.LeftTrigger) && !lastGamePad.IsButtonDown(Buttons.LeftTrigger))
						buttons.Add(Buttons.LeftTrigger);
					if (gamePad.IsButtonDown(Buttons.RightTrigger) && !lastGamePad.IsButtonDown(Buttons.RightTrigger))
						buttons.Add(Buttons.RightTrigger);
					if (gamePad.IsButtonDown(Buttons.X) && !lastGamePad.IsButtonDown(Buttons.X))
						buttons.Add(Buttons.X);
					if (gamePad.IsButtonDown(Buttons.Y) && !lastGamePad.IsButtonDown(Buttons.Y))
						buttons.Add(Buttons.Y);
					if (gamePad.IsButtonDown(Buttons.Start) && !lastGamePad.IsButtonDown(Buttons.Start))
						buttons.Add(Buttons.Start);

					if (buttons.Count > 0)
					{
						if (this.AnyInputDown.Bindings.Count > 0)
						{
							for (int i = 0; i < buttons.Count; i++)
								this.AnyInputDown.Execute(new PCInputBinding { GamePadButton = buttons[i] });
						}
						if (this.nextInputListeners.Count > 0)
							this.notifyNextInputListeners(new PCInputBinding { GamePadButton = buttons[0] });
					}
				}

				for (int i = 0; i < this.buttons.Count; i++)
				{
					Buttons button = this.buttons[i];
					bool newValue = gamePad.IsButtonDown(button);
					Property<bool> prop = this.buttonProperties[button];
					if (prop != newValue)
					{
						prop.Value = newValue;
						if (!this.preventKeyDownEvents)
						{
							if (newValue)
							{
								Command command;
								if (buttonDownCommands.TryGetValue(button, out command))
									command.Execute();
							}
							else
							{
								Command command;
								if (buttonUpCommands.TryGetValue(button, out command))
									command.Execute();
							}
						}
					}
				}
			}

			if (!this.chordActivated && !this.preventKeyDownEvents)
			{
				if (keys.Length == 2)
				{
					Chord chord = new Chord();
					if (keys[1] == Keys.LeftAlt || keys[1] == Keys.LeftControl || keys[1] == Keys.LeftShift || keys[1] == Keys.LeftWindows
						|| keys[1] == Keys.RightAlt || keys[1] == Keys.RightControl || keys[1] == Keys.RightShift || keys[1] == Keys.RightWindows)
					{
						chord.Modifier = keys[1];
						chord.Key = keys[0];
					}
					else
					{
						chord.Modifier = keys[0];
						chord.Key = keys[1];
					}

					Command chordCommand;
					if (this.chords.TryGetValue(chord, out chordCommand))
					{
						chordCommand.Execute();
						this.chordActivated = true;
					}
				}
			}
			else if (keyboard.GetPressedKeys().Length == 0)
				this.chordActivated = false;

			if (this.EnableMouse)
			{
				MouseState mouse = this.main.MouseState;
				this.handleMouse();

				bool newLeftMouseButton = mouse.LeftButton == ButtonState.Pressed;
				if (newLeftMouseButton != this.LeftMouseButton)
				{
					this.LeftMouseButton.Value = newLeftMouseButton;
					if (!this.preventKeyDownEvents)
					{
						if (newLeftMouseButton)
						{
							if (!this.temporarilyIgnoreLMB)
							{
								if (this.nextInputListeners.Count > 0)
									this.notifyNextInputListeners(new PCInputBinding { MouseButton = MouseButton.LeftMouseButton });
								this.LeftMouseButtonDown.Execute();
								this.AnyInputDown.Execute(new PCInputBinding { MouseButton = MouseButton.LeftMouseButton });
							}
						}
						else
						{
							if (this.temporarilyIgnoreLMB)
								this.temporarilyIgnoreLMB = false;
							else
								this.LeftMouseButtonUp.Execute();
						}
					}
				}

				bool newMiddleMouseButton = mouse.MiddleButton == ButtonState.Pressed;
				if (newMiddleMouseButton != this.MiddleMouseButton)
				{
					this.MiddleMouseButton.Value = newMiddleMouseButton;
					if (!this.preventKeyDownEvents)
					{
						if (newMiddleMouseButton)
						{
							if (this.nextInputListeners.Count > 0)
								this.notifyNextInputListeners(new PCInputBinding { MouseButton = MouseButton.MiddleMouseButton });
							this.MiddleMouseButtonDown.Execute();
							this.AnyInputDown.Execute(new PCInputBinding { MouseButton = MouseButton.MiddleMouseButton });
						}
						else
							this.MiddleMouseButtonUp.Execute();
					}
				}

				bool newRightMouseButton = mouse.RightButton == ButtonState.Pressed;
				if (newRightMouseButton != this.RightMouseButton)
				{
					this.RightMouseButton.Value = newRightMouseButton;
					if (!this.preventKeyDownEvents)
					{
						if (newRightMouseButton)
						{
							if (this.nextInputListeners.Count > 0)
								this.notifyNextInputListeners(new PCInputBinding { MouseButton = MouseButton.RightMouseButton });
							this.RightMouseButtonDown.Execute();
							this.AnyInputDown.Execute(new PCInputBinding { MouseButton = MouseButton.RightMouseButton });
						}
						else
							this.RightMouseButtonUp.Execute();
					}
				}

				int newScrollWheel = mouse.ScrollWheelValue;
				int oldScrollWheel = this.ScrollWheel;
				if (newScrollWheel != oldScrollWheel)
				{
					this.ScrollWheel.Value = newScrollWheel;
					if (!this.preventKeyDownEvents)
						this.MouseScrolled.Execute(newScrollWheel > oldScrollWheel ? 1 : -1);
				}
			}
			this.preventKeyDownEvents = false;
		}

		public void SwallowEvents()
		{
			this.preventKeyDownEvents = true;
		}

		protected virtual void handleMouse()
		{
			MouseState state = this.main.MouseState, lastState = this.main.LastMouseState;
			if (state.X != lastState.X || state.Y != lastState.Y)
				this.Mouse.Value = new Vector2(state.X, state.Y);
		}
	}
}