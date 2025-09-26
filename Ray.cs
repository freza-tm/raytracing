internal readonly record struct Ray( Vector Origin, Vector Direction )
{
	public Vector At( double t ) => Origin + t * Direction;
}