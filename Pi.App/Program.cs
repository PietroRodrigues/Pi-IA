// Entry point único do Pi-IA. O ProjectReference garante que o BodyTools
// foi compilado; o Controller inicia a TUI, que por sua vez inicia o backend.
using Pi.App.Controller;
using Pi.App.Model;
using Pi.App.View;

var manifest = ModuleManifest.Discover();
var state = new RuntimeState();
var view = new LauncherView(state);
var controller = new RuntimeController(manifest, view, state);

return await controller.RunAsync();
