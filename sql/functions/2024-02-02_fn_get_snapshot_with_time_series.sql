-- DROP FUNCTION public.fn_get_snapshot_with_time_series(uuid, _uuid, _uuid, _uuid, _uuid, _uuid, bool, bool, bool, bool, bool);

CREATE OR REPLACE FUNCTION public.fn_get_snapshot_with_time_series(currentassetid uuid, commandattributeids uuid[], dynamicattributeids uuid[], integrationattributeids uuid[], runtimeattributeids uuid[], staticattributeids uuid[], hasAssetId boolean, hascommandattributeids boolean, hasdynamicattributeids boolean, hasintegrationattributeids boolean, hasruntimeattributeids boolean, hasstaticattributeids boolean)
 RETURNS TABLE(datatype character varying, assetid uuid, attributeid uuid, unixtimestamp numeric, valuetext text, signalqualitycode smallint, lastgoodvaluetext text, lastgoodunixtimestamp numeric)
 LANGUAGE plpgsql
AS $function$
DECLARE
	signal_quality_good smallint = 192;
BEGIN
    RETURN query
    	WITH time_series_cte AS (
			SELECT am.asset_id,
			    am.id AS attribute_id,
			    sn.data_type,
			    COALESCE(sn.value, ''::text) AS value,
			    sn._ts,
			    aat.attribute_type,
			    sn.device_id,
			    sn.metric_key,
			    NULL::uuid AS integration_id,
			    sn.signal_quality_code,
			    sn.last_good_value,
			    sn._lts
			 FROM asset_attribute_dynamic_mapping am
			     JOIN asset_attribute_templates aat ON am.asset_attribute_template_id = aat.id
			     JOIN v_device_metrics sn ON am.device_id::text = sn.device_id::text AND am.metric_key::text = sn.metric_key::text
		     where hasDynamicAttributeids = true and ((hasAssetId = true and am.asset_id = currentassetid) or hasAssetId = false) and am.id = ANY(dynamicAttributeids)
			UNION ALL
			 SELECT am.asset_id,
			    am.id AS attribute_id,
			    aat.data_type,
			    COALESCE(am.value, ''::text) AS value,
			    am._ts,
			    aat.attribute_type,
			    am.device_id,
			    am.metric_key,
			    NULL::uuid AS integration_id,
			    NULL::smallint AS signal_quality_code,
			    NULL::text AS last_good_value,
			    NULL::timestamp without time zone AS _lts
			  FROM asset_attribute_command_mapping am
			     JOIN asset_attribute_templates aat ON am.asset_attribute_template_id = aat.id
			  where hascommandAttributeids = true and ((hasAssetId = true and am.asset_id = currentassetid) or hasAssetId = false) and am.id = ANY(commandAttributeids)
			UNION ALL
			 SELECT am.asset_id,
			    am.id AS attribute_id,
			    aat.data_type,
			    COALESCE(sn.value, ''::text) AS value,
			    sn._ts,
			    aat.attribute_type,
			    NULL::character varying AS device_id,
			    NULL::character varying AS metric_key,
			    NULL::uuid AS integration_id,
			    NULL::smallint AS signal_quality_code,
			    NULL::text AS last_good_value,
			    NULL::timestamp without time zone AS _lts
			   FROM asset_attribute_runtime_mapping am
			     JOIN asset_attribute_templates aat ON am.asset_attribute_template_id = aat.id
			     LEFT JOIN asset_attribute_runtime_snapshots sn ON am.id = sn.asset_attribute_id AND am.asset_id = sn.asset_id
			   where hasRuntimeAttributeids = true and ((hasAssetId = true and am.asset_id = currentassetid) or hasAssetId = false) and am.id = ANY(runtimeAttributeids)
			UNION ALL
			 SELECT am.asset_id,
			    am.id AS attribute_id,
			    aat.data_type,
			    COALESCE(sn.value, ''::text) AS value,
			    sn._ts,
			    aat.attribute_type,
			    COALESCE(am.device_id, sn.device_id) AS device_id,
			    COALESCE(am.metric_key, sn.metric_key) AS metric_key,
			    COALESCE(am.integration_id, sn.integration_id) AS integration_id,
			    NULL::smallint AS signal_quality_code,
			    NULL::text AS last_good_value,
			    NULL::timestamp without time zone AS _lts
			   FROM asset_attribute_integration_mapping am
			     JOIN asset_attribute_templates aat ON am.asset_attribute_template_id = aat.id
			     LEFT JOIN device_metric_external_snapshots sn ON am.device_id::text = sn.device_id::text AND am.integration_id = sn.integration_id AND am.metric_key::text = sn.metric_key::text
			   where hasIntegrationAttributeids = true and ((hasAssetId = true and am.asset_id = currentassetid) or hasAssetId = false) and am.id = ANY(integrationAttributeids)
			UNION ALL
			 SELECT am.asset_id,
			    am.id AS attribute_id,
			    aat.data_type,
			    COALESCE(am.value::character varying(1024), ''::character varying) AS value,
			    am.updated_utc::timestamp without time zone AS _ts,
			    aat.attribute_type,
			    NULL::character varying AS device_id,
			    NULL::character varying AS metric_key,
			    NULL::uuid AS integration_id,
			    signal_quality_good AS signal_quality_code,
			    NULL::text AS last_good_value,
			    NULL::timestamp without time zone AS _lts
			   FROM asset_attribute_static_mapping am
			     JOIN asset_attribute_templates aat ON am.asset_attribute_template_id = aat.id
			   where hasStaticAttributeids = true and ((hasAssetId = true and am.asset_id = currentassetid) or hasAssetId = false) and am.id = ANY(staticAttributeids)
			UNION ALL
			 SELECT aa.asset_id,
			    aad.asset_attribute_id AS attribute_id,
			    sn.data_type,
			    COALESCE(sn.value, ''::text) AS value,
			    sn._ts,
			    aa.attribute_type,
			    sn.device_id,
			    sn.metric_key,
			    NULL::uuid AS integration_id,
			    sn.signal_quality_code,
			    sn.last_good_value,
			    sn._lts
			   FROM asset_attribute_dynamic aad
			     JOIN asset_attributes aa ON aad.asset_attribute_id = aa.id
			     JOIN v_device_metrics sn ON aad.device_id::text = sn.device_id::text AND aad.metric_key::text = sn.metric_key::text
			  where hasDynamicAttributeids = true and ((hasAssetId = true and aa.asset_id = currentassetid) or hasAssetId = false) and aa.attribute_type::text = 'dynamic'::text and aad.asset_attribute_id = ANY(dynamicAttributeids)
			UNION ALL
			 SELECT aa.asset_id,
			    aad.asset_attribute_id AS attribute_id,
			    aa.data_type,
			    COALESCE(aad.value, ''::text) AS value,
			    aad._ts,
			    aa.attribute_type,
			    aad.device_id,
			    aad.metric_key,
			    NULL::uuid AS integration_id,
			    NULL::smallint AS signal_quality_code,
			    NULL::text AS last_good_value,
			    NULL::timestamp without time zone AS _lts
			   FROM asset_attribute_commands aad
			     JOIN asset_attributes aa ON aad.asset_attribute_id = aa.id
			  WHERE hascommandAttributeids = true and ((hasAssetId = true and aa.asset_id = currentassetid) or hasAssetId = false) and aa.attribute_type::text = 'command'::text and aad.asset_attribute_id = ANY(commandAttributeids)
			UNION ALL
			 SELECT aad.asset_id,
			    aad.id AS attribute_id,
			    aad.data_type,
			    COALESCE(sn.value, ''::text) AS value,
			    sn._ts,
			    aad.attribute_type,
			    NULL::character varying AS device_id,
			    NULL::character varying AS metric_key,
			    NULL::uuid AS integration_id,
			    NULL::smallint AS signal_quality_code,
			    NULL::text AS last_good_value,
			    NULL::timestamp without time zone AS _lts
			   FROM asset_attributes aad
			     LEFT JOIN asset_attribute_runtime_snapshots sn ON aad.id = sn.asset_attribute_id
			  where hasRuntimeAttributeids = true and ((hasAssetId = true and aad.asset_id = currentassetid) or hasAssetId = false) and aad.attribute_type::text = 'runtime'::text and aad.id = ANY(runtimeAttributeids)
			UNION ALL
			 SELECT aad.asset_id,
			    aad.id AS attribute_id,
			    aad.data_type,
			    COALESCE(aad.value::character varying(1024), ''::character varying) AS value,
			    aad.updated_utc::timestamp without time zone AS _ts,
			    aad.attribute_type,
			    NULL::character varying AS device_id,
			    NULL::character varying AS metric_key,
			    NULL::uuid AS integration_id,
			    signal_quality_good AS signal_quality_code,
			    NULL::text AS last_good_value,
			    NULL::timestamp without time zone AS _lts
			   FROM asset_attributes aad
			  where hasStaticAttributeids = true and ((hasAssetId = true and aad.asset_id = currentassetid) or hasAssetId = false) and aad.attribute_type::text = 'static'::text and aad.id = ANY(staticAttributeids)
		)
	       select
	            msf.data_type as DataType,
	            msf.asset_id as AssetId,
	            msf.attribute_id as AttributeId,
	            extract(epoch from msf._ts) * 1000 AS UnixTimestamp,
	            msf.value as ValueText,
	            msf.signal_quality_code as SignalQualityCode,
	            msf.last_good_value as LastGoodValueText,
	            extract(epoch from msf._lts) * 1000 AS LastGoodUnixTimestamp
	        FROM time_series_cte msf;
   
END $function$
;
