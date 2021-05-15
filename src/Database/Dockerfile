FROM mcr.microsoft.com/mssql/server:2017-latest-ubuntu

COPY CompanyName.MyMeetings.Database/Scripts/CreateDatabase_Linux.sql /scripts/
COPY entrypoint.sh .
RUN chmod +x entrypoint.sh
ENTRYPOINT ["./entrypoint.sh"]