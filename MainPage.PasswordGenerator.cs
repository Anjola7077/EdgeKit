using System.Security.Cryptography;

namespace EdgeKit;

public partial class MainPage
{
    private View BuildPasswordGenerator()
    {
        var page = Page();

        page.Children.Add(PageHeader(
            "PRIVACY & SECURITY",
            "Password Studio",
            "Create strong passwords without an internet connection."));

        page.Children.Add(Btn(
            "‹  Back to tools",
            () => ShowTab("Tools")));

        int passwordLength = 16;

        var lengthLabel = Txt("16 characters", 15, true, Accent);

        var lengthSlider = new Slider
        {
            Minimum = 8,
            Maximum = 64,
            Value = 16,
            MinimumTrackColor = Accent,
            MaximumTrackColor = SurfaceAlt,
            ThumbColor = Accent
        };

        var uppercase = new CheckBox { IsChecked = true, Color = Accent };
        var lowercase = new CheckBox { IsChecked = true, Color = Accent };
        var numbers = new CheckBox { IsChecked = true, Color = Accent };
        var symbols = new CheckBox { IsChecked = true, Color = Accent };
        var excludeAmbiguous = new CheckBox { IsChecked = true, Color = Accent };

        var passwordEntry = new Entry
        {
            Placeholder = "Generate a password",
            IsReadOnly = true,
            IsPassword = true,
            FontSize = 17,
            TextColor = Ink,
            PlaceholderColor = Muted,
            BackgroundColor = SurfaceAlt
        };

        var showPassword = new CheckBox
        {
            IsChecked = false,
            Color = Accent
        };

        var strengthLabel = Txt(
            "Not generated",
            14,
            true,
            Muted);

        var strengthDetail = Txt(
            "Your password will be evaluated locally.",
            12,
            color: Muted);

        string generatedPassword = "";

        string GetCharacterSet()
        {
            bool exclude = excludeAmbiguous.IsChecked;

            string chars = "";

            if (uppercase.IsChecked)
                chars += exclude
                    ? "ABCDEFGHJKLMNPQRSTUVWXYZ"
                    : "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            if (lowercase.IsChecked)
                chars += exclude
                    ? "abcdefghijkmnopqrstuvwxyz"
                    : "abcdefghijklmnopqrstuvwxyz";

            if (numbers.IsChecked)
                chars += exclude
                    ? "23456789"
                    : "0123456789";

            if (symbols.IsChecked)
                chars += "!@#$%^&*()-_=+[]{};:,.?";

            return chars;
        }

        void UpdateStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                strengthLabel.Text = "Not generated";
                strengthLabel.TextColor = Muted;
                strengthDetail.Text =
                    "Your password will be evaluated locally.";
                return;
            }

            int score = 0;

            if (password.Length >= 12) score++;
            if (password.Length >= 16) score++;
            if (password.Any(char.IsUpper)) score++;
            if (password.Any(char.IsLower)) score++;
            if (password.Any(char.IsDigit)) score++;
            if (password.Any(c => !char.IsLetterOrDigit(c))) score++;

            string rating;
            string detail;

            if (score <= 2)
            {
                rating = "Weak";
                detail = "Consider increasing length and character variety.";
                strengthLabel.TextColor = Color.FromArgb("#E05A5A");
            }
            else if (score <= 4)
            {
                rating = "Moderate";
                detail = "Good start. More length and variety can improve it.";
                strengthLabel.TextColor = Color.FromArgb("#D69B35");
            }
            else
            {
                rating = "Strong";
                detail = "Good length and character variety.";
                strengthLabel.TextColor = Color.FromArgb("#299D72");
            }

            strengthLabel.Text = $"Strength: {rating}";
            strengthDetail.Text = detail;
        }

        void GeneratePassword()
        {
            string characterSet = GetCharacterSet();

            if (string.IsNullOrEmpty(characterSet))
            {
                passwordEntry.Text = "";
                generatedPassword = "";

                strengthLabel.Text = "Choose a character type";
                strengthLabel.TextColor = Muted;
                strengthDetail.Text =
                    "Enable at least one character category.";

                return;
            }

            var requiredGroups = new List<string>();

            bool exclude = excludeAmbiguous.IsChecked;

            if (uppercase.IsChecked)
                requiredGroups.Add(exclude
                    ? "ABCDEFGHJKLMNPQRSTUVWXYZ"
                    : "ABCDEFGHIJKLMNOPQRSTUVWXYZ");

            if (lowercase.IsChecked)
                requiredGroups.Add(exclude
                    ? "abcdefghijkmnopqrstuvwxyz"
                    : "abcdefghijklmnopqrstuvwxyz");

            if (numbers.IsChecked)
                requiredGroups.Add(exclude
                    ? "23456789"
                    : "0123456789");

            if (symbols.IsChecked)
                requiredGroups.Add("!@#$%^&*()-_=+[]{};:,.?");

            if (passwordLength < requiredGroups.Count)
            {
                strengthLabel.Text = "Length too short";
                strengthDetail.Text =
                    "Increase the length to include every selected category.";
                return;
            }

            var password = new List<char>();

            foreach (string group in requiredGroups)
            {
                password.Add(
                    group[RandomNumberGenerator.GetInt32(group.Length)]);
            }

            while (password.Count < passwordLength)
            {
                password.Add(
                    characterSet[
                        RandomNumberGenerator.GetInt32(characterSet.Length)]);
            }

            for (int i = password.Count - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);

                (password[i], password[j]) =
                    (password[j], password[i]);
            }

            generatedPassword = new string(password.ToArray());

            passwordEntry.Text = generatedPassword;
            UpdateStrength(generatedPassword);
        }

        lengthSlider.ValueChanged += (_, e) =>
        {
            passwordLength = (int)Math.Round(e.NewValue);
            lengthSlider.Value = passwordLength;
            lengthLabel.Text = $"{passwordLength} characters";
        };

        showPassword.CheckedChanged += (_, e) =>
        {
            passwordEntry.IsPassword = !e.Value;
        };

        var optionsCard = Card(new VerticalStackLayout
        {
            Spacing = 13,
            Children =
            {
                Txt("PASSWORD LENGTH", 11, true, Accent),
                lengthLabel,
                lengthSlider,

                new BoxView
                {
                    HeightRequest = 1,
                    BackgroundColor = SurfaceAlt
                },

                Txt("CHARACTER TYPES", 11, true, Accent),

                new HorizontalStackLayout
                {
                    Spacing = 10,
                    Children =
                    {
                        uppercase,
                        Txt("Uppercase letters (A-Z)")
                    }
                },

                new HorizontalStackLayout
                {
                    Spacing = 10,
                    Children =
                    {
                        lowercase,
                        Txt("Lowercase letters (a-z)")
                    }
                },

                new HorizontalStackLayout
                {
                    Spacing = 10,
                    Children =
                    {
                        numbers,
                        Txt("Numbers (0-9)")
                    }
                },

                new HorizontalStackLayout
                {
                    Spacing = 10,
                    Children =
                    {
                        symbols,
                        Txt("Symbols (!@#$...)")
                    }
                },

                new BoxView
                {
                    HeightRequest = 1,
                    BackgroundColor = SurfaceAlt
                },

                new HorizontalStackLayout
                {
                    Spacing = 10,
                    Children =
                    {
                        excludeAmbiguous,
                        new VerticalStackLayout
                        {
                            Spacing = 3,
                            Children =
                            {
                                Txt("Exclude ambiguous characters", 13, true),
                                Txt("Avoid characters such as 0, O, 1 and l.",
                                    11, color: Muted)
                            }
                        }
                    }
                }
            }
        });

        var passwordCard = Card(new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                Txt("YOUR PASSWORD", 11, true, Accent),
                passwordEntry,

                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        showPassword,
                        Txt("Show password", 12, color: Muted)
                    }
                },

                new BoxView
                {
                    HeightRequest = 1,
                    BackgroundColor = SurfaceAlt
                },

                strengthLabel,
                strengthDetail
            }
        });

        page.Children.Add(optionsCard);
        page.Children.Add(passwordCard);

        page.Children.Add(Btn(
            "Generate password",
            GeneratePassword,
            true));

        page.Children.Add(Btn(
            "Copy password",
            async () =>
            {
                if (string.IsNullOrEmpty(generatedPassword))
                {
                    await MessageAsync(
                        "No password",
                        "Generate a password before copying.");
                    return;
                }

                await Clipboard.Default.SetTextAsync(
                    generatedPassword);

                await MessageAsync(
                    "Copied",
                    "Your password has been copied to the clipboard.");
            }));

        page.Children.Add(Txt(
            "Security note: Passwords are generated locally using cryptographically secure random numbers. They are not automatically stored by EdgeKit.",
            12,
            color: Muted));

        GeneratePassword();

        return Scroll(page);
    }

    private Task OpenPasswordGeneratorAsync()
    {
        _content.Content = BuildPasswordGenerator();
        return Task.CompletedTask;
    }
}
