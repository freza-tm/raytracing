using System;

namespace RTree;

/// <summary>
/// Represents a check for N-dimensional volume used during R-Tree search
/// </summary>
public interface ICheck
{
	/// <summary>
	/// Returns value indicating whether the given volume is to be processed during R-Tree search
	/// </summary>
	/// <param name="min">The N-dimensional coordinate of volume minimum point</param>
	/// <param name="max">The N-dimensional coordinate of volume maximum point</param>
	bool Check( ReadOnlySpan<double> min, ReadOnlySpan<double> max );
}
