using System;
using UnityEngine;

namespace Com.Innogames.Core.Frontend.NodeDependencyLookup
{
	/**
	 * Serializer for the AssetDependencyCache
	 * Since all json solutions where to slow and the structure is quite simple it was fastest to manually write it into a byte array
	 */
	public class AssetDependencyCacheSerializer
	{
		public const string EOF = "EndOfSerializedAssetDependencyCache";

		public static byte[] Serialize(AssetNode[] assetNodes)
		{
			byte[] bytes = new byte[CacheSerializerUtils.ARRAY_SIZE_OFFSET];
			int offset = 0;

			int length = assetNodes.Length;
			bytes[offset++] = (byte) length;
			bytes[offset++] = (byte) (length >> 8);
			
			foreach (AssetNode assetNode in assetNodes)
			{
				CacheSerializerUtils.EncodeString(assetNode.AssetId, ref bytes, ref offset);

				bytes[offset++] = (byte)assetNode.ResolverDatas.Count;
				bytes[offset++] = (byte)(assetNode.ResolverDatas.Count >> 8);
				
				for (var i = 0; i < assetNode.ResolverDatas.Count; i++)
				{
					AssetNode.ResolverData data = assetNode.ResolverDatas[i];
					long timeStamp = data.TimeStamp;

					for (int k = 0; k < 8; ++k)
					{
						bytes[offset++] = (byte) (timeStamp >> (8 * k));
					}
					
					CacheSerializerUtils.EncodeString(data.Id, ref bytes, ref offset);

					Dependency[] dependencies = data.Dependencies;
					
					bytes[offset++] = (byte) dependencies.Length;
					bytes[offset++] = (byte) (dependencies.Length >> 8);
					
					for (var k = 0; k < dependencies.Length; k++)
					{
						Dependency dependency = dependencies[k];
						CacheSerializerUtils.EncodeString(dependency.Id, ref bytes, ref offset);
						CacheSerializerUtils.EncodeString(dependency.ConnectionType, ref bytes, ref offset);
						CacheSerializerUtils.EncodeString(dependency.NodeType, ref bytes, ref offset);

						int pathLength = dependency.PathSegments.Length;
						bytes[offset++] = (byte) pathLength;
						bytes[offset++] = (byte) (pathLength >> 8);
						
						for (var p = 0; p < pathLength; p++)
						{
							PathSegment pathSegment = dependency.PathSegments[p];
							
							CacheSerializerUtils.EncodeString(pathSegment.Name, ref bytes, ref offset);
							bytes[offset++] = (byte) pathSegment.Type;
						}
						
						bytes = CacheSerializerUtils.EnsureSize(bytes, offset);
					}

					bytes = CacheSerializerUtils.EnsureSize(bytes, offset);
				}
			}
			
			CacheSerializerUtils.EncodeString(EOF, ref bytes, ref offset);
			
			return bytes;
		}
		
		public static AssetNode[] Deserialize(byte[] bytes)
		{
			int offset = 0;
			int nodeLength = bytes[offset++] + (bytes[offset++] << 8);
			
			AssetNode[] assetsNodes = new AssetNode[nodeLength];

			for (int n = 0; n < nodeLength; ++n)
			{
				string guid = CacheSerializerUtils.DecodeString(ref bytes, ref offset);
				AssetNode assetNode = new AssetNode(guid);
				int resLength = bytes[offset++] + (bytes[offset++] << 8);
				
				for (var i = 0; i < resLength; i++)
				{
					AssetNode.ResolverData data = new AssetNode.ResolverData();
					long timeStamp = 0;

					for (int k = 0; k < 8; ++k)
					{
						timeStamp += (long)bytes[offset++] << (8 * k);
					}

					data.TimeStamp = timeStamp;
					data.Id = CacheSerializerUtils.DecodeString(ref bytes, ref offset);

					int dependencyLength = bytes[offset++] + (bytes[offset++] << 8);
					Dependency[] dependencies = new Dependency[dependencyLength];
					
					for (var k = 0; k < dependencyLength; k++)
					{
						string id = CacheSerializerUtils.DecodeString(ref bytes, ref offset);
						string connectionType = CacheSerializerUtils.DecodeString(ref bytes, ref offset);
						string nodeType = CacheSerializerUtils.DecodeString(ref bytes, ref offset);

						int pathLength = bytes[offset++] + (bytes[offset++] << 8);
						PathSegment[] pathSegments = new PathSegment[pathLength];
						
						for (var p = 0; p < pathLength; p++)
						{
							PathSegment pathSegment = new PathSegment();
							
							pathSegment.Name = CacheSerializerUtils.DecodeString(ref bytes, ref offset);
							pathSegment.Type = (PathSegmentType)bytes[offset++];

							pathSegments[p] = pathSegment;
						}
						
						Dependency dependency = new Dependency(id, connectionType, nodeType, pathSegments);

						dependencies[k] = dependency;
					}

					data.Dependencies = dependencies;
					assetNode.ResolverDatas.Add(data);
				}
				
				assetsNodes[n] = assetNode;
			}
			
			string eof = CacheSerializerUtils.DecodeString(ref bytes, ref offset);
			if (!eof.Equals(EOF))
			{
				Debug.LogError("AssetDependencyCache cache file to be corrupted. Rebuilding cache required");
				return new AssetNode[0];
			}

			return assetsNodes;
		}
	}
}