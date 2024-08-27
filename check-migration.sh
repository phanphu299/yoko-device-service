retryCount=100
currentRetry=0
error=1
while test $currentRetry -lt $retryCount
do
if [ $( docker ps -a -f status=exited | grep :migration- | wc -l ) -eq $( docker ps -a | grep :migration- | wc -l ) ]; 
then
  error=0
  break;
else
    echo 'waiting migration to be finished'
    echo 'total:' $(docker ps -a | grep :migration- | wc -l)
    echo 'completed:' $(docker ps -a -f status=exited | grep :migration- | wc -l)
    ((currentRetry=currentRetry+1))
    sleep 1;
fi
done
if test $error -eq 1;
then
  # error
  echo 'Retry Timeout';
  exit 1;
fi
echo 'checking db migration done';