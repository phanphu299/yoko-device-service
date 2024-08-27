using AHI.Infrastructure.Service.Tag.Model;
using Device.Domain.Entity;

namespace Device.Persistence.Constant
{
    public static class SQLConstants
    {
        public static string GET_DEVICE_SELECT =
             $@"d.id,
                d.name,
                d.status,
                d.created_utc,
                d.updated_utc,
                d.retention_days,
                d.enable_health_check,
                d.signal_quality_code,
                dt.id,
                dt.name,
                dt.created_by,
                dt.created_utc,
                dt.updated_utc,
                dt.deleted,
                dt.total_metric,
                d.id,
                d.id as device_id,
                dms._ts as timestamp,
                acm.command_data_timestamps as command_data_timestamp,
                dms.status,
                tag.tag_id";
        public static string GET_DEVICE_SCRIPT = $@"
            SELECT
                /**select**/
            FROM devices d
            LEFT JOIN entity_tags tag ON d.id = tag.entity_id_varchar
            INNER JOIN (
                SELECT id, ROW_NUMBER() OVER() AS rownum
                FROM({{{{tag_pagging_query}}}}) as t
            ) AS main ON d.id = main.id
            INNER JOIN device_templates dt
                ON d.device_template_id = dt.id AND dt.deleted != true
            /**innerjoin**/
            LEFT JOIN (
                SELECT 
                    d.id as device_id, max(dms._ts) AS _ts,
                    CASE
                        WHEN (d.device_content ~~ '%BROKER_EMQX_COAP%'::text OR d.device_content ~~ '%BROKER_EMQX_MQTT%'::text) AND d.device_content ~~ '%""password"":""%'::text
                            THEN 'RG'
                        WHEN d.device_content ~~ '%iot.azure-devices.net%'::text
                            THEN 'RG'
                        WHEN max(dms._ts)
                            IS NOT NULL THEN 'AC'
                            ELSE 'CR'
                    END as status
                FROM devices d
                LEFT JOIN device_metric_snapshots dms ON dms.device_id = d.id
                GROUP BY d.id, d.device_content
            ) dms
                ON dms.device_id = d.id
            LEFT JOIN (
                SELECT DISTINCT asset_attribute_command_histories.device_id, now() AS command_data_timestamps
                FROM asset_attribute_command_histories
            ) acm
                ON acm.device_id = d.id
            /**leftjoin**/
            /**where**/
            ORDER BY main.rownum, tag.Id
            ";
        public static string GET_ASSET_TEMPLATE_SCRIPT = $@"SELECT * FROM (
            SELECT
                at.id as {nameof(AssetTemplate.Id)},
                at.name as {nameof(AssetTemplate.Name)},
                at.created_utc as {nameof(AssetTemplate.CreatedUtc)},
                at.updated_utc as {nameof(AssetTemplate.UpdatedUtc)},
                at.created_by as {nameof(AssetTemplate.CreatedBy)},
                aat.id as {nameof(AssetAttributeTemplate.Id)},
                aat.name as {nameof(AssetAttributeTemplate.Name)},
                aat.value as {nameof(AssetAttributeTemplate.Value)},
                aat.attribute_type as {nameof(AssetAttributeTemplate.AttributeType)},
                aat.data_type as {nameof(AssetAttributeTemplate.DataType)},
                aat.created_utc as {nameof(AssetAttributeTemplate.CreatedUtc)},
                aat.updated_utc as {nameof(AssetAttributeTemplate.UpdatedUtc)},
                aat.uom_id as {nameof(AssetAttributeTemplate.UomId)},
                aat.thousand_separator as {nameof(AssetAttributeTemplate.ThousandSeparator)},
                aat.decimal_place as {nameof(AssetAttributeTemplate.DecimalPlace)},
                aat.sequential_number as {nameof(AssetAttributeTemplate.SequentialNumber)},
                aat.asset_template_id as {nameof(AssetAttributeTemplate.AssetTemplateId)},
                a.id as {nameof(Asset.Id)},
                aati.id as {nameof(AssetAttributeTemplateIntegration.Id)},
                aati.integration_markup_name as {nameof(AssetAttributeTemplateIntegration.IntegrationMarkupName)},
                aati.integration_id as {nameof(AssetAttributeTemplateIntegration.IntegrationId)},
                aati.device_markup_name as {nameof(AssetAttributeTemplateIntegration.DeviceMarkupName)},
                aati.device_id as {nameof(AssetAttributeTemplateIntegration.DeviceId)},
                aati.metric_key as {nameof(AssetAttributeTemplateIntegration.MetricKey)},
                aatd.id as {nameof(AssetAttributeDynamicTemplate.Id)},
                aatd.device_template_id as {nameof(AssetAttributeDynamicTemplate.DeviceTemplateId)},
                aatd.metric_key as {nameof(AssetAttributeDynamicTemplate.MetricKey)},
                aatd.markup_name as {nameof(AssetAttributeDynamicTemplate.MarkupName)},
                aatr.id as {nameof(AssetAttributeRuntimeTemplate.Id)},
                aatr.enabled_expression as {nameof(AssetAttributeRuntimeTemplate.EnabledExpression)},
                aatr.expression as {nameof(AssetAttributeRuntimeTemplate.Expression)},
                aatr.expression_compile as {nameof(AssetAttributeRuntimeTemplate.ExpressionCompile)},
                aatr.trigger_attribute_id as {nameof(AssetAttributeRuntimeTemplate.TriggerAttributeId)},
                aatc.id as {nameof(AssetAttributeCommandTemplate.Id)},
                aatc.device_template_id as {nameof(AssetAttributeCommandTemplate.DeviceTemplateId)},
                aatc.metric_key as {nameof(AssetAttributeCommandTemplate.MetricKey)},
                aatc.markup_name as {nameof(AssetAttributeCommandTemplate.MarkupName)},
                et.entity_id_uuid as {nameof(EntityTag.EntityIdGuid)},
                et.tag_id as {nameof(EntityTag.TagId)}
            FROM asset_templates at
            LEFT JOIN asset_attribute_templates aat
                ON at.id = aat.asset_template_id
            LEFT JOIN assets a
                ON at.id = a.asset_template_id
            LEFT JOIN asset_attribute_template_integrations aati
                ON aat.id = aati.asset_attribute_template_id
            LEFT JOIN asset_attribute_template_dynamics aatd
                ON aat.id = aatd.asset_attribute_template_id
            LEFT JOIN asset_attribute_template_runtimes aatr
                ON aat.id = aatr.asset_attribute_template_id
            LEFT JOIN asset_attribute_template_commands aatc
                ON aat.id = aatc.asset_attribute_template_id
            LEFT JOIN entity_tags et
                ON (at.id = et.entity_id_uuid AND et.entity_type = '{nameof(AssetTemplate)}')
            WHERE at.id = @assetTemplateId
            ORDER BY et.Id
        ) a";
    }
}
