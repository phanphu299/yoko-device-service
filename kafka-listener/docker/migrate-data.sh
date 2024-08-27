#!/bin/sh
if [ "$CREATE_DATABASE" == "false" ]
then
   echo "rh --connectionstring=$CONNECTION_STRING --sqlfilesdirectory=sql --environmentnames=$ENVIRONMENT --databasetype=$DATABASE_TYPE --donotcreatedatabase --silent --withtransaction"
   rh "--connectionstring=$CONNECTION_STRING" --sqlfilesdirectory=sql --environmentnames=$ENVIRONMENT --databasetype=$DATABASE_TYPE --donotcreatedatabase --silent --withtransaction
else
   echo "rh --connectionstring=$CONNECTION_STRING --sqlfilesdirectory=sql --environmentnames=$ENVIRONMENT --databasetype=$DATABASE_TYPE --silent --withtransaction"
   rh "--connectionstring=$CONNECTION_STRING" --sqlfilesdirectory=sql --environmentnames=$ENVIRONMENT --databasetype=$DATABASE_TYPE --silent --withtransaction
fi
echo "rh --connectionstring=$CONNECTION_STRING --sqlfilesdirectory=sql/no-trans --environmentnames=$ENVIRONMENT --databasetype=$DATABASE_TYPE --silent"
rh "--connectionstring=$CONNECTION_STRING" --sqlfilesdirectory=sql/no-trans --environmentnames=$ENVIRONMENT --databasetype=$DATABASE_TYPE --silent
