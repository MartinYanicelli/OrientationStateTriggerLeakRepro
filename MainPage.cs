namespace OrientationStateTriggerLeakRepro;

public class MainPage : ContentPage
{
	public const string UseTriggerKey = "UseOrientationStateTrigger";

	readonly Label resultLabel;

	public MainPage()
	{
		var sequence = PageTracker.Register(this);
		var useTrigger = Preferences.Default.Get(UseTriggerKey, true);
		PageTracker.Log($"Page #{sequence} created, trigger={(useTrigger ? "ON" : "OFF")}");

		Title = $"Page #{sequence}";

		var itemsView = new ItemsView(useTrigger);

		resultLabel = new Label { Text = "Tap \"Collect and count\".", FontSize = 18 };

		var collectButton = new Button { Text = "Collect and count" };
		collectButton.Clicked += OnCollectClicked;

		var triggerSwitch = new Switch { IsToggled = useTrigger };
		triggerSwitch.Toggled += (_, e) => Preferences.Default.Set(UseTriggerKey, e.Value);

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Padding = 20,
				Spacing = 16,
				Children =
				{
					new Label { Text = $"Page #{sequence} (trigger {(useTrigger ? "ON" : "OFF")})", FontSize = 22, FontAttributes = FontAttributes.Bold },
					itemsView,
					collectButton,
					resultLabel,
					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							triggerSwitch,
							new Label { Text = "Use OrientationStateTrigger", VerticalOptions = LayoutOptions.Center },
						},
					},
					new Label
					{
						Text = "The switch takes effect on the next window (next Activity recreation or app restart).",
						FontSize = 13,
					},
				},
			},
		};
	}

	async void OnCollectClicked(object? sender, EventArgs e)
	{
		for (var i = 0; i < 3; i++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			// Give the Android GC bridge time to release peers between passes.
			await Task.Delay(200);
		}

		var (alive, created, aliveSequences) = PageTracker.Count();
		var line = $"Alive pages: {alive} (created: {created})";
		resultLabel.Text = line;
		PageTracker.Log($"{line} alive=[{aliveSequences}]");
	}
}
