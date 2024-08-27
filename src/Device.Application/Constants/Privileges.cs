using System;

namespace Device.Application.Constant
{
    public static class Privileges
    {
        public const string BASE_PATH = "tenants/{{tenantId}}/subscriptions/{{subscriptionId}}/applications/{{applicationId}}/projects/{{projectId}}/entities";
        public const string PRIVILEGES_PATH = "privileges";
        public const string PRIVILEGES_OBJECTS = "objects";
        public const string PRIVILEGES_NONE = "none";
        public const string PRIVILEGES_PROJECTS = "projects";

        public static string GetBasePath(Guid tenantId, Guid subscriptionId, string projectId)
        {
            return $"tenants/{tenantId}/subscriptions/{subscriptionId}/applications/{ApplicationInformation.APPLICATION_ID}/projects/{projectId}/entities";
        }

        public static class Asset
        {
            public const string ENTITY_NAME = "asset";
            public static class Rights
            {
                public const string WRITE_ASSET = "write_asset";
                public const string READ_ASSET = "read_asset";
                public const string READ_CHILD_ASSET = "read_child_asset";
                public const string DELETE_ASSET = "delete_asset";
                public const string ASSIGN_ASSET = "assign_asset";
            }
            public static class FullRights
            {
                //public static string WRITE_ASSET = $"{ApplicationInformation.APPLICATION_ID}/{Asset.ENTITY_NAME}/{Rights.WRITE_ASSET}";
                public const string READ_ASSET = "a0f1c338-1eff-40ff-997e-64f08e141b06/asset/read_asset";
                public const string WRITE_ASSET = "a0f1c338-1eff-40ff-997e-64f08e141b06/asset/write_asset";
                //public const string AD_READ_ASSET = "ea8f57b2-f183-4acc-88b0-249ecb59286e/asset/read_asset";
                //public static string READ_CHILD_ASSET = "entities/asset/objects/*/privileges/read_child_asset";
            }
            public static class Paths
            {
                public const string CHILDREN = "children";
            }
        }

        public static class AssetTemplate
        {
            public const string ENTITY_NAME = "asset_template";
            public static class Rights
            {
                public const string WRITE_ASSET_TEMPLATE = "write_asset_template";
                public const string DELETE_ASSET_TEMPLATE = "delete_asset_template";
                public const string READ_ASSET_TEMPLATE = "read_asset_template";
            }
            public static class FullRights
            {
                public const string READ_ASSET_TEMPLATE = "a0f1c338-1eff-40ff-997e-64f08e141b06/asset_template/read_asset_template";
                public const string WRITE_ASSET_TEMPLATE = "a0f1c338-1eff-40ff-997e-64f08e141b06/asset_template/write_asset_template";
                public const string DELETE_ASSET_TEMPLATE = "a0f1c338-1eff-40ff-997e-64f08e141b06/asset_template/delete_asset_template";
            }
        }

        public static class Device
        {
            public const string ENTITY_NAME = "device";
            public static class Rights
            {
                public const string ASSIGN_DEVICE = "assign_device";
                public const string WRITE_DEVICE = "write_device";
                public const string DELETE_DEVICE = "delete_device";
                public const string READ_DEVICE = "read_device";
            }
            public static class FullRights
            {
                public const string READ_DEVICE = "a0f1c338-1eff-40ff-997e-64f08e141b06/device/read_device";
                public const string WRITE_DEVICE = "a0f1c338-1eff-40ff-997e-64f08e141b06/device/write_device";
                public const string ASSIGN_DEVICE = "a0f1c338-1eff-40ff-997e-64f08e141b06/device/assign_device";
                public const string DELETE_DEVICE = "a0f1c338-1eff-40ff-997e-64f08e141b06/device/delete_device";
            }
        }

        public static class DeviceTemplate
        {
            public const string ENTITY_NAME = "device_template";
            public static class Rights
            {
                public const string WRITE_DEVICE_TEMPLATE = "write_device_template";
                public const string DELETE_DEVICE_TEMPLATE = "delete_device_template";
                public const string READ_DEVICE_TEMPLATE = "read_device_template";
            }
            public static class FullRights
            {
                public const string WRITE_DEVICE_TEMPLATE = "a0f1c338-1eff-40ff-997e-64f08e141b06/device_template/write_device_template";
                public const string DELETE_DEVICE_TEMPLATE = "a0f1c338-1eff-40ff-997e-64f08e141b06/device_template/delete_device_template";
                public const string READ_DEVICE_TEMPLATE = "a0f1c338-1eff-40ff-997e-64f08e141b06/device_template/read_device_template";
            }
        }

        public static class Uom
        {
            public const string ENTITY_NAME = "uom";
            public static class Rights
            {
                public const string WRITE_UOM = "write_uom";
                public const string DELETE_UOM = "delete_uom";
                public const string READ_UOM = "read_uom";
            }
            public static class FullRights
            {
                public const string WRITE_UOM = "a0f1c338-1eff-40ff-997e-64f08e141b06/uom/write_uom";
                public const string DELETE_UOM = "a0f1c338-1eff-40ff-997e-64f08e141b06/uom/delete_uom";
                public const string READ_UOM = "a0f1c338-1eff-40ff-997e-64f08e141b06/uom/read_uom";
            }
        }
        public static class AssetAttribute
        {
            public const string ENTITY_NAME = "asset_attribute";
            public static class Rights
            {
                public const string WRITE_ASSET_ATTRIBUTE = "write_asset_attribute";
                public const string READ_ASSET_ATTRIBUTE = "read_asset_attribute";
                public const string DELETE_ASSET_ATTRIBUTE = "delete_asset_attribute";
            }
            public static class FullRights
            {
                //public static string WRITE_ASSET = $"{ApplicationInformation.APPLICATION_ID}/{Asset.ENTITY_NAME}/{Rights.WRITE_ASSET}";
                public const string READ_ASSET_ATTRIBUTE = "a0f1c338-1eff-40ff-997e-64f08e141b06/asset_attribute/read_asset_attribute";
                public const string WRITE_ASSET_ATTRIBUTE = "a0f1c338-1eff-40ff-997e-64f08e141b06/asset_attribute/write_asset_attribute";
            }
        }
        public static class AssetTable
        {
            public const string ENTITY_NAME = "asset_table";
            public static class Rights
            {
                public const string READ_ASSET_TABLE = "read_asset_table";
                public const string WRITE_ASSET_TABLE = "write_asset_table";
                public const string DELETE_ASSET_TABLE = "delete_asset_table";
            }
        }

        public static class Configuration
        {
            public const string ENTITY_NAME = "asset_configuration";
            public static class Rights
            {
                public const string SHARE_CONFIGURATION = "share_asset_configuration";
            }
            public static class FullRights
            {
                public const string SHARE_CONFIGURATION = "a0f1c338-1eff-40ff-997e-64f08e141b06/asset_configuration/share_asset_configuration";
            }
        }

        public static class EventForwarding
        {
            public const string ENTITY_NAME = "event";
            public static class Rights
            {
                public const string READ_EVENT_FORWARDING = "read_event";
            }
            public static class FullRights
            {
                public const string READ_EVENT_FORWARDING = "a0f1c338-1eff-40ff-997e-64f08e141b06/event/read_event";
            }
        }
        public static class AlarmRule
        {
            public static class FullRights
            {
                public const string READ_ALARM_RULE = "entities/alarm_rule/objects/*/privileges/read_alarm_rule";
            }
        }
        public static class BlockTemplate
        {
            public const string ENTITY_NAME = "block_template";
            public static class Rights
            {
                public const string WRITE_BLOCK_TEMPLATE = "write_block_template";
                public const string DELETE_BLOCK_TEMPLATE = "delete_block_template";
                public const string READ_BLOCK_TEMPLATE = "read_block_template";
            }
            public static class FullRights
            {
                public const string WRITE_BLOCK_TEMPLATE = "a0f1c338-1eff-40ff-997e-64f08e141b06/block_template/write_block_template";
                public const string READ_BLOCK_TEMPLATE = "a0f1c338-1eff-40ff-997e-64f08e141b06/block_template/read_block_template";
                public const string DELETE_BLOCK_TEMPLATE = "a0f1c338-1eff-40ff-997e-64f08e141b06/block_template/delete_block_template";
            }
        }
        public static class BlockExecution
        {
            public const string ENTITY_NAME = "block_execution";
            public static class Rights
            {
                public const string WRITE_BLOCK_EXECUTION = "write_block_execution";
                public const string DELETE_BLOCK_EXECUTION = "delete_block_execution";
                public const string READ_BLOCK_EXECUTION = "read_block_execution";
            }
            public static class FullRights
            {
                public const string WRITE_BLOCK_EXECUTION = "a0f1c338-1eff-40ff-997e-64f08e141b06/block_execution/write_block_execution";
                public const string READ_BLOCK_EXECUTION = "a0f1c338-1eff-40ff-997e-64f08e141b06/block_execution/read_block_execution";
                public const string DELETE_BLOCK_EXECUTION = "a0f1c338-1eff-40ff-997e-64f08e141b06/block_execution/delete_block_execution";
            }
        }

        public static class ReportTemplate
        {
            public const string ENTITY_NAME = "report_template";

            public static class Rights
            {
                public const string READ_REPORT_TEMPLATE = "read_report_template";
                public const string WRITE_REPORT_TEMPLATE = "write_report_template";
                public const string DELETE_REPORT_TEMPLATE = "delete_report_template";
            }

            public static class FullRights
            {
                public const string READ_REPORT_TEMPLATE = "entities/report_template/objects/*/privileges/read_report_template";
                public const string WRITE_REPORT_TEMPLATE = "entities/report_template/objects/*/privileges/write_report_template";
                public const string DELETE_REPORT_TEMPLATE = "entities/report_template/objects/*/privileges/delete_report_template";
            }
        }
    }
}
