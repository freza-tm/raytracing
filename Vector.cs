internal readonly record struct Vector( double X, double Y, double Z )
{
	public static Vector operator -( Vector a ) => new( -a.X, -a.Y, -a.Z );
	public static Vector operator -( Vector a, Vector b ) => new( a.X - b.X, a.Y - b.Y, a.Z - b.Z );
	public static Vector operator +( Vector a, Vector b ) => new( a.X + b.X, a.Y + b.Y, a.Z + b.Z );
	public static Vector operator *( Vector a, double f ) => new( a.X * f, a.Y * f, a.Z * f );
	public static Vector operator *( double f, Vector a ) => new( a.X * f, a.Y * f, a.Z * f );
	public static Vector operator /( Vector a, double d ) => a * (1 / d);

	public double Dot( Vector other ) => X * other.X + Y * other.Y + Z * other.Z;

	public Vector Cross( Vector other ) => new( Y * other.Z - Z * other.Y, Z * other.X - X * other.Z, X * other.Y - Y * other.X );

	public Vector Hadamard( Vector other ) => new( X * other.X, Y * other.Y, Z * other.Z );

	public double MagnitudeSquared => X * X + Y * Y + Z * Z;
	public double Magnitude => Math.Sqrt( MagnitudeSquared );

	public bool NearZero => Math.Abs( X ) < 1e-6 && Math.Abs( Y ) < 1e-6 && Math.Abs( Z ) < 1e-6;

	public bool IsInvalid => double.IsNaN( X ) && double.IsNaN( Y ) && double.IsNaN( Z );

	public Vector Normalized() => this / Magnitude;

	public Vector Reflect( Vector normal ) => this - 2 * this.Dot( normal ) * normal;

	public Vector Refract( Vector normal, double etaIoverEtaT )
	{
		var cosTheta = Math.Min( Dot( -normal ), 1.0 );
		var rOutPependicular = etaIoverEtaT * (this + cosTheta * normal);
		var rOutParallel = -Math.Sqrt( Math.Abs( 1.0 - rOutPependicular.MagnitudeSquared ) ) * normal;

		return rOutPependicular + rOutParallel;
	}

	public static Vector Invalid() => new Vector( double.NaN, double.NaN, double.NaN );

	public static Vector Random() => new Vector( System.Random.Shared.NextDouble(), System.Random.Shared.NextDouble(), System.Random.Shared.NextDouble() );

	public static Vector RandomSymmetric() => new Vector( 2 * System.Random.Shared.NextDouble() - 1, 2 * System.Random.Shared.NextDouble() - 1, 2 * System.Random.Shared.NextDouble() - 1 );

	public static Vector RandomNormalized()
	{
		while( true )
		{
			var candidate = RandomSymmetric();

			if( candidate.MagnitudeSquared > 1.0 || candidate.MagnitudeSquared < 1e-160 )
				continue;

			return candidate.Normalized();
		}
	}

	public static Vector RandomInDisc()
	{
		while( true )
		{
			var candidate = new Vector( 2 * System.Random.Shared.NextDouble() - 1, 2 * System.Random.Shared.NextDouble() - 1, 0 );

			if( candidate.MagnitudeSquared > 1.0 )
				continue;

			return candidate;
		}
	}
}