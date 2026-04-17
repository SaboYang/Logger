using System;
using Microsoft.Extensions.Logging;

namespace Logger.Extensions.Logging.Demo
{
    public sealed class DemoActionService
    {
        private readonly ILogger<DemoActionService> _logger;

        public DemoActionService(ILogger<DemoActionService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void EmitGreeting()
        {
            _logger.LogInformation("DemoActionService 已通过 ILogger<T> 注入。");
        }

        public void EmitSampleBatch(int count)
        {
            _logger.LogInformation("开始输出批量日志，数量 {Count}", count);

            for (int index = 1; index <= count; index++)
            {
                _logger.LogDebug("批量项 {Index}/{Count}", index, count);
            }

            _logger.LogInformation("批量日志输出完成，数量 {Count}", count);
        }

        public void EmitException()
        {
            try
            {
                throw new InvalidOperationException("Demo 异常示例");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "异常示例已捕获");
            }
        }
    }
}
