using LogApi.Models;
namespace LogApi.Services;

public interface ILogService
{
    IEnumerable<List<LogDataModel>> GetLogs();
    void AddLog(LogDataModel log);
    void AddLogWithoutReplication(LogDataModel log);
}