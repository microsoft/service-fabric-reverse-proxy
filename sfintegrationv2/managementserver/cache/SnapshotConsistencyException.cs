// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in 
//  the repo root for license information.
// ------------------------------------------------------------

using System;

namespace Cache
{
    /**
     * SnapshotConsistencyException indicates that resource references in a Snapshot are not consistent,
     * i.e. a resource references another resource that does not exist in the snapshot.
     */
    public class SnapshotConsistencyException : Exception
    {
        public SnapshotConsistencyException(string message) : base(message)
        {
        }
    }
}
