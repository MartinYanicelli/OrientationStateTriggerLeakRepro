namespace OrientationStateTriggerLeakRepro;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	// No Shell, no DI: every Activity recreation creates a new Window with a new MainPage.
	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new MainPage());
	}
}
