// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in 
//  the repo root for license information.
// ------------------------------------------------------------

using Google.Protobuf;
using System.Collections.Generic;

namespace Cache
{
    public class SnapshotResources<T> where T : IMessage
    {
        public SnapshotResources(Dictionary<string, IMessage> resources, string version)
        {
            this.Resources = resources;
            this.Version = version;
        }

        /**
* Returns a new {@link SnapshotResources} instance.
*
* @param resources the resources in this collection
* @param version the version associated with the resources in this collection
* @param <T> the type of resources in this collection
*/
        public static SnapshotResources<T> create(IEnumerable<T> resources, string version)
        {
            Dictionary<string, IMessage> d = new Dictionary<string, IMessage>();

            foreach (var r in resources)
            {
                d.Add(Cache.Resources.GetResourceName(r), r);
            }

            return new SnapshotResources<T>(d, version);
        }

        /**
         * Returns a dicitonary of the resources in this collection, where the key is the name of the resource.
         */
        public Dictionary<string, IMessage> Resources { get; }

        /**
         * Returns the version associated with this resources in this collection.
         */
        public string Version { get; }
    }
}
