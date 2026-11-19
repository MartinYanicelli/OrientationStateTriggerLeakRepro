namespace OrientationStateTriggerLeakRepro;

public partial class ItemsView : ContentView
{
	public ItemsView(bool useTrigger)
	{
		InitializeComponent();

		// Control: same XAML, but the state triggers are removed before the view is attached to a window.
		if (!useTrigger)
			VisualStateManager.GetVisualStateGroups(Items).Clear();

		BindableLayout.SetItemsSource(Items, new[] { 1, 2, 3 });
	}
}
