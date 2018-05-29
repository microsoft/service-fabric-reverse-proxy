// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in 
//  the repo root for license information.
// ------------------------------------------------------------

using Envoy.Api.V2.Core;

namespace Cache
{
    /**
     * {@code NodeGroup} aggregates config resources by a consistent grouping of {@link Node}s.
     */
    public interface INodeGroup<T>
    {

        /**
         * Returns a consistent identifier of the given {@link Node}.
         *
         * @param node identifier for the envoy instance that is requesting config
         */
        T Hash(Node node);
    }

    class SingleNodeGroup : INodeGroup<string>
    {

        public static string GROUP = "node";
        public string Hash(Node node)
        {
            if (node == null)
            {
                throw new System.ArgumentException("node");
            }

            return GROUP;
        }
    }

}
