namespace Phy6Sim
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

			// Register routes for each simulation page
			Routing.RegisterRoute(nameof(ZeroGravityPage), typeof(ZeroGravityPage));
			Routing.RegisterRoute(nameof(LiquidSimPage), typeof(LiquidSimPage));
			Routing.RegisterRoute(nameof(RagdollPage), typeof(RagdollPage));
			Routing.RegisterRoute(nameof(ChainPhysicsPage), typeof(ChainPhysicsPage));
			Routing.RegisterRoute(nameof(StarfieldPage), typeof(StarfieldPage));
		}
    }
}
