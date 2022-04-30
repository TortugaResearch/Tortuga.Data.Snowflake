/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Tortuga.Data.Snowflake.Core;
using System.Data;
using System.Data.Common;

namespace Tortuga.Data.Snowflake
{
    public class SnowflakeDbParameter : DbParameter
    {
        public SFDataType SFDataType { get; set; }

        private SFDataType OriginType;

        public SnowflakeDbParameter()
        {
            SFDataType = SFDataType.None;
            OriginType = SFDataType.None;
        }

        public SnowflakeDbParameter(string ParameterName, SFDataType SFDataType)
        {
            this.ParameterName = ParameterName;
            this.SFDataType = SFDataType;
            OriginType = SFDataType;
        }

        public SnowflakeDbParameter(int ParameterIndex, SFDataType SFDataType)
        {
            this.ParameterName = ParameterIndex.ToString();
            this.SFDataType = SFDataType;
        }

        public override DbType DbType { get; set; }

        public override ParameterDirection Direction
        {
            get
            {
                return ParameterDirection.Input;
            }

            set
            {
                if (value != ParameterDirection.Input)
                {
                    throw new SnowflakeDbException(SFError.UNSUPPORTED_FEATURE);
                }
            }
        }

        public override bool IsNullable { get; set; }

        public override string ParameterName { get; set; }

        public override int Size { get; set; }

        public override string SourceColumn { get; set; }

        public override bool SourceColumnNullMapping { get; set; }

        public override object Value { get; set; }

        public override void ResetDbType()
        {
            SFDataType = OriginType;
        }
    }
}