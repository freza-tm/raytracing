using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

internal sealed class OutputImage( int width, int height ) : IDisposable
{
	private Vector[] _image = new Vector[width * height];
	private WriteableBitmap _bitmap = new( new PixelSize( width, height ), new Avalonia.Vector( 96, 96 ), PixelFormat.Rgba8888, AlphaFormat.Opaque );

	public void Dispose() => _bitmap?.Dispose();

	public event EventHandler? BitmapUpdated;

	public void SaveToFile( string fileName )
	{
		_bitmap?.Save( fileName );
	}

	public IImage Image => _bitmap;

	public bool DumpBitmap { get; set; }
	public int Divisor { get; private set; }

	public void AddPixels( Func<int, IEnumerable<Vector>> pixelsProducer )
	{
		if( Divisor == 0 )
		{
			Array.Fill<Vector>( _image, new Vector() );
		}

		Parallel.ForEach( Enumerable.Range( 0, height ), GenerateRow );

		void GenerateRow( int row )
		{
			var rowData = _image.AsSpan<Vector>().Slice( row * width, width );
			int i = 0;
			foreach( var pixel in pixelsProducer( row ) )
			{
				rowData[i++] += pixel;
			}
		}

		Divisor++;

		if( DumpBitmap )
		{
			using var data = _bitmap.Lock();

			Span<Pixel> pixels = stackalloc Pixel[width];

			for( int row = 0; row < height; row++ )
			{
				Span<Vector> sourceRow = _image.AsSpan<Vector>()[(row * width)..];
				for( int x = 0; x < width; x++ )
				{
					pixels[x] = new Pixel( sourceRow[x] / Divisor );
				}

				{
					var dstSpanOfBytes = MemoryMarshal.CreateSpan( ref Unsafe.AddByteOffset( ref Unsafe.NullRef<byte>(), data.Address + row * data.RowBytes ), data.RowBytes );

					var dstSpanOfPixels = MemoryMarshal.Cast<byte, Pixel>( dstSpanOfBytes );

					pixels.CopyTo( dstSpanOfPixels );
				}

			}
			DumpBitmap = false;
			BitmapUpdated?.Invoke( this, EventArgs.Empty );
		}
	}
}