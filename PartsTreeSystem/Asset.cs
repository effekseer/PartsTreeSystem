using System;
using System.Collections.Generic;
using System.Text;

namespace PartsTreeSystem
{
	public class Asset
	{
		Difference difference = null;

		internal virtual Difference GetDifference(int instanceID) { return difference; }
		internal virtual void SetDifference(int instanceID, Difference difference) { this.difference = difference; }
	}

}