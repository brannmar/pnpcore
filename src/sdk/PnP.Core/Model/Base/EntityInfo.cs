﻿using System.Collections.Generic;
using System.Linq;

namespace PnP.Core.Model
{
    /// <summary>
    /// Class describing the underlying data store, used to map data store to model. This class contains the dynamic and the static part
    /// </summary>
    internal class EntityInfo
    {
        /// <summary>
        /// Default constructor        
        /// </summary>
        internal EntityInfo()
        {
        }

        /// <summary>
        /// Parameterized constructor        
        /// </summary>
        /// <param name="entityInfo">An instance of EntityInfo to construct the new EntityInfo from</param>
        internal EntityInfo(EntityInfo entityInfo)
        {
            entityInfo.Fields.ForEach(f => Fields.Add((EntityFieldInfo)f.Clone()));

            ActualKeyFieldName = entityInfo.ActualKeyFieldName;
            SharePointType = entityInfo.SharePointType;
            SharePointUri = entityInfo.SharePointUri;
            GraphId = entityInfo.GraphId;
            GraphBeta = entityInfo.GraphBeta;
            GraphGet = entityInfo.GraphGet;
            SharePointGet = entityInfo.SharePointGet;
            GraphLinqGet = entityInfo.GraphLinqGet;
            SharePointLinqGet = entityInfo.SharePointLinqGet;
            UseOverflowField = entityInfo.UseOverflowField;
            SharePointOverflowProperty = entityInfo.SharePointOverflowProperty;
            GraphOverflowProperty = entityInfo.GraphOverflowProperty;
            GraphUpdate = entityInfo.GraphUpdate;
            SharePointUpdate = entityInfo.SharePointUpdate;
            GraphDelete = entityInfo.GraphDelete;
            SharePointDelete = entityInfo.SharePointDelete;
        }

        /// <summary>
        /// Field mapping information
        /// </summary>
        internal List<EntityFieldInfo> Fields { get; private set; } = new List<EntityFieldInfo>();

        /// <summary>
        /// Name of the actual key field
        /// </summary>
        internal string ActualKeyFieldName;

        /// <summary>
        /// Data store type when using REST
        /// </summary>
        internal string SharePointType { get; set; }

        /// <summary>
        /// Uri that uniquely identifies this item via REST
        /// </summary>
        internal string SharePointUri { get; set; }

        /// <summary>
        /// Value of the id field used to load graph relationships (e.g. load lists from a given site)
        /// </summary>
        internal string GraphId { get; set; }

        /// <summary>
        /// Specifies if this class requires the Microsoft Graph beta endpoint
        /// </summary>
        internal bool GraphBeta { get; set; }

        /// <summary>
        /// API call for a Graph get
        /// </summary>
        internal string GraphGet { get; set; }

        /// <summary>
        /// API call for a REST get
        /// </summary>
        internal string SharePointGet { get; set; }

        /// <summary>
        /// API call for a Graph LINQ get
        /// </summary>
        internal string GraphLinqGet { get; set; }

        /// <summary>
        /// API call for a REST LINQ get
        /// </summary>
        internal string SharePointLinqGet { get; set; }

        /// <summary>
        /// Indicates if this class must be handled as a generic dictionary by populating data in the provided field
        /// </summary>
        internal bool UseOverflowField { get; set; }

        /// <summary>
        /// Indicates the property used for the overflow field when a REST query is used
        /// </summary>
        internal string SharePointOverflowProperty { get; set; }

        /// <summary>
        /// Indicates the property used for the overflow field when a Graph query is used
        /// </summary>
        internal string GraphOverflowProperty { get; set; }

        /// <summary>
        /// API call for a Graph update
        /// </summary>
        internal string GraphUpdate { get; set; }

        /// <summary>
        /// API call for a REST update
        /// </summary>
        internal string SharePointUpdate { get; set; }

        /// <summary>
        /// API call for a Graph delete
        /// </summary>
        internal string GraphDelete { get; set; }

        /// <summary>
        /// API call for a REST delete
        /// </summary>
        internal string SharePointDelete { get; set; }

        private EntityFieldInfo _sharePointKeyField;

        /// <summary>
        /// Gets the first field marked as IsKey
        /// </summary>
        internal EntityFieldInfo SharePointKeyField
        {
            get
            {
                if (_sharePointKeyField == null)
                {
                    _sharePointKeyField = Fields.FirstOrDefault(f => f.IsSharePointKey);
                }
                return _sharePointKeyField;
            }
        }

        private EntityFieldInfo _graphKeyField;

        /// <summary>
        /// Gets the first field marked as IsKey
        /// </summary>
        internal EntityFieldInfo GraphKeyField
        {
            get
            {
                if (_graphKeyField == null)
                {
                    _graphKeyField = Fields.FirstOrDefault(f => f.IsGraphKey);
                }
                return _graphKeyField;
            }
        }

        private List<EntityFieldInfo> _graphNonExpandableCollections;

        /// <summary>
        /// Returns a list of fields in this entity which do require a separate query (they can't be expanded)
        /// </summary>
        internal List<EntityFieldInfo> GraphNonExpandableCollections
        {
            get
            {
                if (_graphNonExpandableCollections == null)
                {
                    _graphNonExpandableCollections =
                        Fields.Where(f => !string.IsNullOrEmpty(f.GraphGet)).ToList();
                }
                return _graphNonExpandableCollections;
            }
        }

        /// <summary>
        /// Was there an expression provided to build up the fields lists of this entity
        /// </summary>
        internal bool SharePointFieldsLoadedViaExpression { get; set; } = false;

        /// <summary>
        /// Was there an expression provided to build up the fields lists of this entity
        /// </summary>
        internal bool GraphFieldsLoadedViaExpression { get; set; } = false;

        /// <summary>
        /// Check if Microsoft Graph can be used based upon the requested fields to load
        /// </summary>
        internal bool CanUseGraphGet
        {
            get
            {
                // in case there was no expression used and the entity allows to be fetched via Graph than favor graph
                if (!GraphFieldsLoadedViaExpression && !string.IsNullOrEmpty(GraphGet))
                {
                    return true;
                }

                // get collection of fields that need to be loaded
                return Fields
                    .Where(f => f.Load)
                    .All(f => !string.IsNullOrEmpty(f.GraphName));
            }
        }

    }
}
