using System;
using System.Collections.Generic;
using System.Text;

namespace PartsTreeSystem
{
	public class Asset
	{
		internal virtual Difference GetDifference(int instanceID) { return null; }
		internal virtual void SetDifference(int instanceID, Difference difference) { }
	}

}