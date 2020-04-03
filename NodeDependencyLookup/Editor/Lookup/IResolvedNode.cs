﻿namespace Com.Innogames.Core.Frontend.NodeDependencyLookup
{
	/**
	 * A node that got resolved by one of the caches
	 * This could be for example an Asset, LocaKey, AssetBundle, etc. 
	 */
	public interface IResolvedNode
	{
		string Id { get; }
		string Type { get; }
		
		/**
		 * It can be that a node got cached as being a dependency but is no longer existing anymore.
		 * This would be the case if an asset got deleted which was used by another one.
		 */
		bool Existing { get;  }
	}
}