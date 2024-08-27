-- this script will correct the device status.
-- since we use the status as device real status. The accepted value should be: 
-- AC: Active/Connected
-- IN: InActive/Disconnected
-- NA: Not applicable.
update devices set status = 'NA' where enable_health_check = false;
update devices set status = 'IN' where enable_health_check = true;