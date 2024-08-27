update  devices 
set status = 'AC'
from  (
	select d.id, d.status, dms._ts
	from devices d
	left join ( select device_metric_snapshots.device_id,
            max(device_metric_snapshots._ts) AS _ts
           from device_metric_snapshots
          group by device_metric_snapshots.device_id) dms ON dms.device_id::text = d.id::text
) sub
where devices.id = sub.id and devices.status = 'CR' and sub._ts is not null;

update devices
set _ts = sub._ts
from (
select d.id AS device_id,
    dms._ts
   from devices d
     left join ( select device_metric_snapshots.device_id,
            max(device_metric_snapshots._ts) AS _ts
           from device_metric_snapshots
          group by device_metric_snapshots.device_id) dms ON dms.device_id::text = d.id::text
) sub
where devices.id = sub.device_id