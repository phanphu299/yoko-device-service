namespace AHI.Device.Function.Constant
{
    public static class ErrorMessage
    {
        public static class ErrorProperty
        {
            public const string ERROR_PROPERTY_NAME = "PropertyName";
            public const string ERROR_PROPERTY_VALUE = "PropertyValue";

            public static class Device
            {
                public const string ID = "Identifier";
                public const string NAME = "Name";
                public const string TEMPLATE = "Template";
                public const string RETENTION_DAYS = "Retention Day";
                public const string BROKER_NAME = "Broker";
                public const string SAS_TOKEN_DURATION = "SAS Token Duration";
                public const string TOKEN_DURATION = "Token Duration";
                public const string TELEMETRY_TOPIC = "Telemetry Topic";
                public const string COMMAND_TOPIC = "Command Topic";
            }

            public static class DeviceTemplate
            {
                public const string NAME = "Name";
                public const string DATA_TYPE = "Data Type";
                public const string PAYLOAD = "Payload";
                public const string DETAIL = "Detail";
                public const string DETAILS = "Details";
                public const string KEY = "Key";
                public const string KEY_TYPE = "Key Type";
                public const string EXPRESSION = "Expression";
                public const string DEFAULT_VALUE = "Default Value";
            }

            public static class AssetTemplate
            {
                public const string NAME = "Name";
                public const string DATA_TYPE = "Data Type";
                public const string ATTRIBUTE_NAME = "Attribute Name";
                public const string DEVICE_TEMPLATE = "Device Template";
                public const string CHANNEL = "Channel";
                public const string MARKUP_CHANNEL = "Markup Channel";
                public const string DEVICE = "DeviceID";
                public const string MARKUP_DEVICE = "Markup DeviceID";
                public const string MARKUP_TRIGGER_ASSET = "Markup Trigger Asset ID";
                public const string METRIC = "Metric";
                public const string UOM = "UoM";
                public const string VALUE = "Value";
                public const string ASSET_TEMPLATE = "Asset Template";
                public const string DECIMAL_PLACES = "Decimal Places";
                public const string THOUSAND_SEPARATOR = "Thousand Separator";
            }

            public static class AssetAttribute
            {
                public const string ATTRIBUTE_NAME = "Attribute Name";
                public const string ATTRIBUTE_TYPE = "Type";
                public const string DEVICE_ID = "Device ID";
                public const string CHANNEL = "Channel";
                public const string METRIC = "Metric";
                public const string VALUE = "Value";
                public const string DATA_TYPE = "Data Type";
                public const string ALIAS_ASSET = "Alias Asset";
                public const string ALIAS_ATTRIBUTE = "Alias Attribute";
                public const string ENABLED_EXPRESSION = "Enabled Expression";
                public const string EXPRESSION = "Expression";
                public const string TRIGGER_ATTRIBUTE = "Trigger Attribute";
                public const string UOM = "UoM";
                public const string DECIMAL_PLACES = "Decimal Place";
                public const string THOUSAND_SEPARATOR = "Thousand Separator";
            }

            public static class Uom
            {
                public const string NAME = "Name";
                public const string LOOKUP = "Category";
                public const string ABBREVIATION = "Abbreviation";
                public const string REF_NAME = "Reference UoM";
                public const string REF_FACTOR = "Factor";
                public const string CANONICAL_FACTOR = "Canonical Factor";
                public const string REF_OFFSET = "Offset";
                public const string CANONICAL_OFFSET = "Canonical Offset";
            }

            public static class AttributeTemplate
            {
                public const string ATTRIBUTE_NAME = "Attribute Name";
                public const string TYPE = "Type";
                public const string DEVICE_TEMPLATE = "Device Template/ID";
                public const string CHANNEL = "Channel";
                public const string MARKUP_CHANNEL = "Channel Markup";
                public const string DEVICE_ID = "Device";
                public const string MARKUP_DEVICE = "Device Markup";
                public const string METRIC = "Metric";
                public const string VALUE = "Value";
                public const string DATA_TYPE = "Data Type";
                public const string ENABLED_EXPRESSION = "Enabled Expression";
                public const string EXPRESSION = "Expression";
                public const string UOM = "UoM";
                public const string DECIMAL_PLACE = "Decimal Place";
                public const string THOUSAND_SEPARATOR = "Thousand Separator";
                public const string TRIGGER_ATTRIBUTE = "Trigger Attribute";
            }
        }

        public static class FluentValidation
        {
            public const string REQUIRED = "AUDIT.LOG.IMPORT_ERROR.REQUIRED";
            public const string EQUAL_COMPARISON = "AUDIT.LOG.IMPORT_ERROR.EQUAL_COMPARISON";
            public const string MAX_LENGTH = "AUDIT.LOG.IMPORT_ERROR.MAX_LENGTH";
            public const string MAX_LENGTH_UTF8 = "AUDIT.LOG.IMPORT_ERROR.MAX_LENGTH_UTF8";
            public const string OUT_OF_RANGE_INCLUSIVE = "AUDIT.LOG.IMPORT_ERROR.OUT_OF_RANGE";
            public const string GREATER_THAN_MAX_VALUE = "AUDIT.LOG.IMPORT_ERROR.GREATER_THAN_MAX_VALUE";
            public const string LESS_THAN_MIN_VALUE = "AUDIT.LOG.IMPORT_ERROR.LESS_THAN_MIN_VALUE";
            public const string STRICT_LESS_THAN_MIN_VALUE = "AUDIT.LOG.IMPORT_ERROR.STRICT_LESS_THAN_MIN_VALUE";

            public const string GENERAL_OUT_OF_RANGE = "AUDIT.LOG.IMPORT_ERROR.GENERAL_OUT_OF_RANGE";
            public const string GENERAL_INVALID_VALUE = "AUDIT.LOG.IMPORT_ERROR.GENERAL_INVALID";
            public const string DEVICE_ID_UNMATCHED_BROKER_TYPE_LOG = "ERROR.ENTITY.VALIDATION.ID_UNMATCHED_BROKER_TYPE_LOG";
            public const string NOT_EXIST = "AUDIT.LOG.IMPORT_ERROR.NOT_EXIST";
            public const string CHILD_NOT_EXIST = "AUDIT.LOG.IMPORT_ERROR.CHILD_NOT_EXIST";
            public const string MARKUP_DUPLICATED = "AUDIT.LOG.IMPORT_ERROR.MARKUP_DUPLICATED";
            public const string MARKUP_USED = "AUDIT.LOG.IMPORT_ERROR.MARKUP_USED";
            public const string ALREADY_HAVE_MARKUP = "AUDIT.LOG.IMPORT_ERROR.ALREADY_HAVE_MARKUP";
            public const string METRIC_DUPLICATED = "AUDIT.LOG.IMPORT_ERROR.METRIC_DUPLICATED";
            public const string METRIC_INCONSISTENT = "AUDIT.LOG.IMPORT_ERROR.METRIC_INCONSISTENT";
            public const string PAYLOAD_INVALID = "AUDIT.LOG.IMPORT_ERROR.PAYLOAD_INVALID";
            public const string KEYTYPE_ONLY_ONCE = "AUDIT.LOG.IMPORT_ERROR.KEYTYPE_ONLY_ONCE";
            public const string DETAIL_TYPE_INVALID = "AUDIT.LOG.IMPORT_ERROR.TEMPLATE_DETAIL_TYPE_INVALID";
            public const string DETAIL_METRIC_TYPE_INVALID = "AUDIT.LOG.IMPORT_ERROR.TEMPLATE_DETAIL_METRIC_TYPE_INVALID";
            public const string FORMAT_MISSING_FIELDS = "AUDIT.LOG.IMPORT_ERROR.MISSING_FIELDS";
            public const string BINDING_TYPE_INVALID = "AUDIT.LOG.IMPORT_ERROR.TEMPLATE_BINDING_TYPE_INVALID";
            public const string INGESTION_DEVICEID_NOT_EXIST = "AUDIT.LOG.INGESTION_ERROR.DEVICEID_NOT_EXIST";
            public const string INGESTION_INVALID_DATA_TYPE = "AUDIT.LOG.INGESTION_ERROR.INVALID_DATA_TYPE";
            public const string INGESTION_INVALID_TIMESTAMP = "AUDIT.LOG.INGESTION_ERROR.INVALID_TIMESTAMP";
            public const string INGESTION_INVALID_FORMAT = "AUDIT.LOG.INGESTION_ERROR.INVALID_FORMAT";
            public const string INGESTION_INVALID_CSV_FORMAT = "AUDIT.LOG.INGESTION_ERROR.INVALID_CSV_FORMAT";
            public const string DEVICE_INVALID_TOPIC_FORMAT = "AUDIT.LOG.IMPORT_ERROR.INVALID_TOPIC_FORMAT";
            public const string TAG_KEY_MAX_LENGTH = "AUDIT.LOG.IMPORT_ERROR.TAG.KEY_MAX_LENGTH";
            public const string TAG_VALUE_MAX_LENGTH = "AUDIT.LOG.IMPORT_ERROR.TAG.VALUE_MAX_LENGTH";
            public const string TAG_MAX_LENGTH = "AUDIT.LOG.IMPORT_ERROR.TAG.MAX_LENGTH";
            public const string GET_FILE_FAILED = "AUDIT.LOG.IMPORT_ERROR.GET_FILE_FAILED";
            public const string EXPORT_NOT_SUPPORTED = "AUDIT.LOG.EXPORT_ERROR.NOT_SUPPORTED";
            public const string EXPORT_NOT_FOUND = "AUDIT.LOG.EXPORT_ERROR.NOT_FOUND";
        }

        public static class ParseValidation
        {
            public const string PARSER_GENERAL_INVALID_VALUE = "FILE.PARSE_ERROR.GENERAL_INVALID";
            public const string PARSER_MAX_LENGTH = "FILE.PARSE_ERROR.MAX_LENGTH";
            public const string PARSER_REQUIRED = "FILE.PARSE_ERROR.REQUIRED";
            public const string PARSER_MISSING_COLUMN = "FILE.PARSE_ERROR.MISSING_COLUMN";
            public const string PARSER_MISSING_ATTRIBUTE_TYPE = "FILE.PARSE_ERROR.MISSING_ATTRIBUTE_TYPE";
            public const string PARSER_DUPLICATED_ATTRIBUTE_NAME = "FILE.PARSE_ERROR.DUPLICATED_ATTRIBUTE_NAME";
            public const string PARSER_MANDATORY_FIELDS_REQUIRED = "FILE.PARSE_ERROR.MANDATORY_FIELDS_REQUIRED";
            public const string PARSER_DEPENDENCES_REQUIRED = "FILE.PARSE_ERROR.DEPENDENCES_REQUIRED";
            public const string PARSER_REFERENCES_DATA_NOT_EXISTS = "FILE.PARSE_ERROR.REFERENCES_DATA_NOT_EXISTS";
            public const string PARSER_INVALID_DATA = "FILE.PARSE_ERROR.INVALID_DATA";
        }
    }
}
