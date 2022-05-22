using VisualTests;

using var program = new TestWindow( new() {
	IsMultiThreaded = false,
	RenderFrequency = 999,
	UpdateFrequency = 999
}, new() {
	
} );

program.Run();