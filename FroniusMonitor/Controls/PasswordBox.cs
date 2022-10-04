namespace De.Hochstaetter.FroniusMonitor.Controls
{
    public class PasswordBox : TextBox
    {
        private bool isChangeFromInside;
        private const char PasswordChar = '●';
        private ButtonBase? visibilityButton;
        private VisibilityIcon? visibilityIcon;
        private const string VisibilityButtonName = "ShowHideButton";

        public static DependencyProperty PasswordProperty = DependencyProperty.Register
        (
            nameof(Password), typeof(string), typeof(PasswordBox),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((PasswordBox)d).OnAnyChange())
        );

        public static DependencyProperty VisibleProperty = DependencyProperty.Register
        (
            nameof(Visible), typeof(bool), typeof(PasswordBox),
            new PropertyMetadata((d, _) => ((PasswordBox)d).OnAnyChange())
        );

        public static readonly DependencyProperty ShowPasswordOnClickOnlyProperty = DependencyProperty.Register
        (
            nameof(ShowPasswordOnClickOnly), typeof(bool), typeof(PasswordBox),
            new PropertyMetadata(true)
        );

        public bool ShowPasswordOnClickOnly
        {
            get => (bool)GetValue(ShowPasswordOnClickOnlyProperty);
            set => SetValue(ShowPasswordOnClickOnlyProperty, value);
        }

        public PasswordBox()
        {
            TextChanged += OnTextChanged;
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, DoCopy));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, DoCut));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, DoPaste));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Undo, (_, _) => { }));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Redo, (_, _) => { }));

            Loaded += (_, _) =>
            {
                try
                {
                    visibilityIcon = this.FindVisualChildren<VisibilityIcon>().SingleOrDefault();
                }
                catch (Exception ex)
                {
                    throw new XamlParseException($"The control template of a {nameof(PasswordBox)} must contain exactly one instance of {nameof(VisibilityIcon)}", ex);
                }

                if (visibilityIcon != null)
                {
                    visibilityIcon.Visible = Visible;
                }

                try
                {
                    visibilityButton = this.FindVisualChildren<ButtonBase>().SingleOrDefault(b => b.Name == VisibilityButtonName);
                }
                catch (Exception ex)
                {
                    throw new XamlParseException($"The control template of a {nameof(PasswordBox)} must contain an inherited class of {nameof(ButtonBase)} with a name of {nameof(VisibilityButtonName)}", ex);
                }

                if (visibilityButton != null)
                {
                    visibilityButton.Click += OnShowHideClick;
                    visibilityButton.PreviewMouseDown += OnShowHideMouseUpOrDown;
                    visibilityButton.PreviewMouseUp += OnShowHideMouseUpOrDown;
                    visibilityButton.MouseLeave += OnShowHideMouseEnterOrLeave;
                    visibilityButton.MouseEnter += OnShowHideMouseEnterOrLeave;
                }
            };

            Unloaded += (_, _) =>
            {
                TextChanged -= OnTextChanged;

                if (visibilityButton != null)
                {
                    visibilityButton.PreviewMouseDown -= OnShowHideMouseUpOrDown;
                    visibilityButton.PreviewMouseUp -= OnShowHideMouseUpOrDown;
                    visibilityButton.Click -= OnShowHideClick;
                    visibilityButton.MouseEnter -= OnShowHideMouseEnterOrLeave;
                    visibilityButton.MouseLeave -= OnShowHideMouseEnterOrLeave;
                }
            };
        }

        public string Password
        {
            get => (string)GetValue(PasswordProperty);
            set => SetValue(PasswordProperty, value);
        }

        public bool Visible
        {
            get => (bool)GetValue(VisibleProperty);
            set => SetValue(VisibleProperty, value);
        }

        private void OnShowHideClick(object sender, RoutedEventArgs e)
        {
            if (!ShowPasswordOnClickOnly)
            {
                Visible = !Visible;
                e.Handled = true;
            }
        }

        private void OnShowHideMouseEnterOrLeave(object sender, MouseEventArgs e)
        {
            if (ShowPasswordOnClickOnly)
            {
                Visible = visibilityButton is {IsMouseOver: true} && e.LeftButton == MouseButtonState.Pressed;
            }
        }

        private void OnShowHideMouseUpOrDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || e.Handled || !ShowPasswordOnClickOnly)
            {
                return;
            }

            Visible = e.ButtonState == MouseButtonState.Pressed;
            e.Handled = true;
        }

        private void OnAnyChange()
        {
            UpdateText();

            if (visibilityIcon != null && visibilityIcon.Visible != Visible)
            {
                visibilityIcon.Visible = Visible;
            }
        }

        private void DoCopy(object sender, ExecutedRoutedEventArgs e)
        {
            Clipboard.SetText(Password.Substring(SelectionStart, SelectionLength));
            e.Handled = true;
        }

        private void DoCut(object sender, ExecutedRoutedEventArgs e)
        {
            DoCopy(sender, e);

            if (IsReadOnly)
            {
                return;
            }

            var savedCaretIndex = CaretIndex;
            Password = Password[..SelectionStart] + Password[(SelectionStart + SelectionLength)..];
            CaretIndex = savedCaretIndex;
        }

        private void DoPaste(object sender, ExecutedRoutedEventArgs e)
        {
            if (IsReadOnly)
            {
                return;
            }

            var savedCaretIndex = CaretIndex;
            var clipboardText = Clipboard.GetText();
            Password = Password[..SelectionStart] + clipboardText + Password[(SelectionStart + SelectionLength)..];
            CaretIndex = savedCaretIndex + clipboardText.Length;
            e.Handled = true;
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (isChangeFromInside)
            {
                return;
            }

            if (Visible)
            {
                Password = Text;
                return;
            }

            var savedCaretIndex = CaretIndex;
            var savedText = Text;

            foreach (var change in e.Changes)
            {
                var part1 = Password[..change.Offset];

                if (change.RemovedLength > 0)
                {
                    var part2 = Password[(change.Offset + change.RemovedLength)..];
                    Password = part1 + part2;
                }

                if (change.AddedLength > 0)
                {
                    var part2 = savedText.Substring(change.Offset, change.AddedLength);
                    var part3 = change.Offset + change.AddedLength <= Password.Length ? Password[change.Offset..] : string.Empty;
                    Password = part1 + part2 + part3;
                }
            }

            CaretIndex = savedCaretIndex;
        }

        private void UpdateText()
        {
            isChangeFromInside = true;

            try
            {
                Text = Visible ? Password : new string(PasswordChar, Password?.Length??0);
            }
            finally
            {
                isChangeFromInside = false;
            }
        }
    }
}
