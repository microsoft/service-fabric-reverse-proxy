using Envoy.Api.V2;
using Google.Protobuf;
// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in 
//  the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;
/**
*  Response is a data class that contains the response for an assumed configuration type.
*/
public class Response
{
    public Response(DiscoveryRequest request, List<IMessage> resources, string version)
    {
        this.Request = request;
        this.Resources = resources;
        this.Version = version;
    }

    public static Response Create(DiscoveryRequest request, List<IMessage> resources, string version)
    {
        return new Response(request, resources, version);
    }

    /**
     * Returns the original request associated with the response.
     */
    public DiscoveryRequest Request { get; }

    /**
     * Returns the resources to include in the response.
     */
    public List<IMessage> Resources { get; }

    /**
     * Returns the version of the resources as tracked by the cache for the given type. Envoy responds with this version
     * as an acknowledgement.
     */
    public string Version { get; }
}
