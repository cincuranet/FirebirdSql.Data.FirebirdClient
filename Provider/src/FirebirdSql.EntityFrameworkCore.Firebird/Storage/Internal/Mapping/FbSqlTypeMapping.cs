using FirebirdSql.Data.FirebirdClient;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace Microsoft.EntityFrameworkCore.Storage.Internal
{
    
        public class FbTypeMapping : RelationalTypeMapping
        {
            public FbDbType? fbDbType { get; protected set; }

            internal FbTypeMapping([NotNull] string storeType, [NotNull] Type clrType, FbDbType?  FbDbTypeTemp = null)
                : base(storeType, clrType, unicode: false, size: null, dbType: null)
            { 
             fbDbType = FbDbTypeTemp;
            }

            protected override void ConfigureParameter([NotNull] DbParameter parameter)
            {
                if (fbDbType.HasValue)
                    ((FbParameter)parameter).FbDbType = fbDbType.Value;
            }

            public override RelationalTypeMapping Clone(string storeType, int? size)
                => new FbTypeMapping(storeType, ClrType, fbDbType);
        
    }
}
