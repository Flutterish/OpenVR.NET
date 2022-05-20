using VisualTests;

using var program = new TestWindow( new() {
	IsMultiThreaded = false,
	RenderFrequency = 120,
	UpdateFrequency = 240
}, new() {
	
} );

program.Run();