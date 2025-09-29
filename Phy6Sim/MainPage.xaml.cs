namespace Phy6Sim;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}

	private async void Sim1Button_Clicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(ZeroGravityPage));
	}

	private async void Sim2Button_Clicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(LiquidSimPage));
	}

	private async void Sim3Button_Clicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(RagdollPage));
	}

	private async void Sim4Button_Clicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(ChainPhysicsPage));
	}

	private async void Sim5Button_Clicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(StarfieldPage));
	}
}