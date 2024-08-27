retryCount=100
currentRetry=0
error=1
while test $currentRetry -lt $retryCount
do
if [ $( curl --location --request POST 'http://localhost:8081/connect/token' --header 'Content-Type: application/x-www-form-urlencoded' --data-urlencode 'grant_type=client_credentials' --data-urlencode 'client_id=postman-sa' --data-urlencode 'client_secret=CU8cEU3yJ4hCEd6QAB' | grep access_token | wc -l ) -eq 1 ]; 
then
  error=0
  break;
else
    echo 'waiting identity-service to be ready'
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
echo 'identity-service is ready';
echo $(curl --location --request POST 'http://localhost:8081/connect/token' --header 'Content-Type: application/x-www-form-urlencoded' --data-urlencode 'grant_type=client_credentials' --data-urlencode 'client_id=postman-sa' --data-urlencode 'client_secret=CU8cEU3yJ4hCEd6QAB')

# ---- tenants
currentRetry=0
error=1
while test $currentRetry -lt $retryCount
do
if [ $( curl --location --request GET 'http://localhost:7073/fnc/mst/tenants?code=localhost' | grep id | wc -l ) -eq 1 ];
then
  error=0
  break;
else
    echo 'waiting master-function/tenants to be ready'
     ((currentRetry=currentRetry+1))
    sleep 1;
fi
done
if test $error -eq 1;
then
  # error
  echo 'Retry Timeout';
  exit 1
fi
echo 'master-function/tenants is ready';
echo $(curl --location --request GET 'http://localhost:7073/fnc/mst/tenants?code=localhost')

# ---- subscriptions
currentRetry=0
error=1
while test $currentRetry -lt $retryCount
do
if [ $( curl --location --request GET 'http://localhost:7073/fnc/mst/subscriptions?code=localhost' | grep id | wc -l ) -eq 1 ]; 
then
  error=0
  break;
else
    echo 'waiting master-function/subscriptions to be ready'
     ((currentRetry=currentRetry+1))
    sleep 1;
fi
done
if test $error -eq 1;
then
  # error
  echo 'Retry Timeout';
  exit 1
fi

echo 'master-function/subscriptions is ready';
echo $(curl --location --request GET 'http://localhost:7073/fnc/mst/subscriptions?code=localhost')

# ---- projects
currentRetry=0
error=1
while test $currentRetry -lt $retryCount
do
if [ $( curl --location --request GET 'http://localhost:7073/fnc/mst/projects?code=localhost' | grep id | wc -l ) -eq 1 ]; 
then
  error=0
  break;
else
    echo 'waiting master-function/projects to be ready'
    ((currentRetry=currentRetry+1))
    sleep 1;
fi
done
if test $error -eq 1;
then
  # error
  echo 'Retry Timeout';
  exit 1
fi
echo 'master-function/projects is ready';
echo $(curl --location --request GET 'http://localhost:7073/fnc/mst/projects?code=localhost')