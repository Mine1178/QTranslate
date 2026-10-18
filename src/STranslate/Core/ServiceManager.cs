using Microsoft.Extensions.Logging;
using STranslate.Plugin;
using System.IO;

namespace STranslate.Core;

public class ServiceManager
{
    private readonly PluginManager _pluginManager;
    private readonly ServiceSettings _serviceSettings;
    private readonly ILogger<ServiceManager> _logger;
    private readonly List<Service> _services;

    public ServiceManager(PluginManager pluginManager, ServiceSettings serviceSettings, ILogger<ServiceManager> logger)
    {
        _pluginManager = pluginManager;
        _serviceSettings = serviceSettings;
        _logger = logger;
        _services = [];

        Directory.CreateDirectory(DataLocation.PluginSettingsDirectory);
    }
    public IEnumerable<Service> AllServices => _services;

    public void LoadServices()
    {
        // 遍历所有插件目录，删除服务配置目录和缓存目录
        var directories = new string[] { DataLocation.PluginSettingsDirectory, DataLocation.PluginCacheDirectory }
            .SelectMany(Directory.EnumerateDirectories);

        foreach (var directory in directories)
        {
            if (Helper.ShouldDeleteDirectory(directory))
            {
                Helper.TryDeleteDirectory(directory);
                continue;
            }
        }

        var svcJsonNames = new List<string>();

        var serviceDataCollections = new List<ServiceData>[]
        {
            _serviceSettings.TranSvcDatas,
            _serviceSettings.OcrSvcDatas,
            _serviceSettings.TtsSvcDatas,
            _serviceSettings.VocabularySvcDatas,
        };

        foreach (var metaData in _pluginManager.AllPluginMetaDatas)
        {
            var combineName = Helper.GetPluginDicrtoryName(metaData);
            var serviceSettingPath = Path.Combine(DataLocation.PluginSettingsDirectory, combineName);
            if (!Directory.Exists(serviceSettingPath))
                continue;

            /*
             * 当前方案: xxPlugin/35d9d684683245e680a5308c801ca2ad.json
             * 待定方案: xxPlugin/35d9d684683245e680a5308c801ca2ad/Settings.json
             * xxPlugin/35d9d684683245e680a5308c801ca2ad/Other.json
             */
            // 获取目录下所有json文件名
            var jsonFileNames = Directory
                .EnumerateFiles(serviceSettingPath, "*.json", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileNameWithoutExtension)
                .OfType<string>()
                .ToList();
            svcJsonNames.AddRange(jsonFileNames);

            foreach (var serviceDataCollection in serviceDataCollections)
            {
                /* 
                 * 只有同时存在配置文件(.json)和服务配置数据(ServiceData)的服务才会被加载
                 * 代码等同于 LINQ 查询:
                 * var result = from svc in _serviceSettings.TranSvcDatas
                 *              join fileName in jsonFileNames on svc.SvcID equals fileName
                 *              select svc;
                 */
                jsonFileNames
                    .Join(serviceDataCollection,
                            fileName => fileName,
                            serviceData => serviceData.SvcID,
                            (fileName, serviceData) => serviceData)
                    .ToList()
                    .ForEach(item =>
                    {
                        var service = CreateService(metaData, item);
                        service.Initialize();
                        _services.Add(service);
                    });

            }
        }

        var svcSettingJsons = serviceDataCollections.SelectMany(item => item.Select(x => x.SvcID)).ToList();
        var lossSvcs = svcSettingJsons.Except(svcJsonNames);
        if (lossSvcs.Any())
        {
            _serviceSettings.TranSvcDatas.RemoveAll(s => lossSvcs.Contains(s.SvcID));
            _serviceSettings.OcrSvcDatas.RemoveAll(s => lossSvcs.Contains(s.SvcID));
            _serviceSettings.TtsSvcDatas.RemoveAll(s => lossSvcs.Contains(s.SvcID));
            _serviceSettings.VocabularySvcDatas.RemoveAll(s => lossSvcs.Contains(s.SvcID));
            _serviceSettings.Save();

            // 删除丢失服务对应的JSON文件，并清理空目录
            TryCleanupLostServiceFiles(lossSvcs);

            _logger.LogWarning($"发现丢失的服务配置，已自动移除: {string.Join(", ", lossSvcs)}");
        }

        if (!_serviceSettings.TranSvcDatas.Select(x => x.SvcID).Contains(_serviceSettings.ReplaceSvcID))
        {
            _serviceSettings.ReplaceSvcID = string.Empty;
            _serviceSettings.Save();

            _logger.LogInformation("替换服务已重置为空，因为之前设置的服务不存在。");
        }

        if (!_serviceSettings.TranSvcDatas.Select(x => x.SvcID).Contains(_serviceSettings.ImageTranslateSvcID))
        {
            _serviceSettings.ImageTranslateSvcID = string.Empty;
            _serviceSettings.Save();

            _logger.LogInformation("图片翻译服务已重置为空，因为之前设置的服务不存在。");
        }

        if (!_serviceSettings.OcrSvcDatas.Select(x => x.SvcID).Contains(_serviceSettings.ImageTranslateOcrSvcID))
        {
            _serviceSettings.ImageTranslateOcrSvcID = string.Empty;
            _serviceSettings.Save();

            _logger.LogInformation("图片翻译OCR服务已重置为空，因为之前设置的服务不存在。");
        }

        // 默认服务自动初始化（首次启动时）
        EnsureDefaultServices();
    }

    /// <summary>
    /// 首次启动时自动添加默认服务
    /// </summary>
    private void EnsureDefaultServices()
    {
        // 默认翻译服务：金山词霸内置
        if (_serviceSettings.TranSvcDatas.Count == 0)
        {
            var iciba = _pluginManager.AllPluginMetaDatas
                .FirstOrDefault(p => p.PluginType != null &&
                    (typeof(ITranslatePlugin).IsAssignableFrom(p.PluginType) ||
                     typeof(IDictionaryPlugin).IsAssignableFrom(p.PluginType)) &&
                    p.Name.Contains("金山词霸"));
            if (iciba != null)
            {
                AddService(iciba, ServiceType.Translation);
                _logger.LogInformation("已自动添加默认翻译服务: 金山词霸");
            }
        }

        // 默认TTS服务：Edge TTS
        if (_serviceSettings.TtsSvcDatas.Count == 0)
        {
            var edgeTts = _pluginManager.AllPluginMetaDatas
                .FirstOrDefault(p => p.PluginType != null &&
                    typeof(ITtsPlugin).IsAssignableFrom(p.PluginType) &&
                    p.Name.Contains("Edge"));
            if (edgeTts != null)
            {
                AddService(edgeTts, ServiceType.TTS);
                _logger.LogInformation("已自动添加默认TTS服务: Edge TTS");
            }
        }

        // 默认生词本服务：欧路词典
        if (_serviceSettings.VocabularySvcDatas.Count == 0)
        {
            var eudict = _pluginManager.AllPluginMetaDatas
                .FirstOrDefault(p => p.PluginType != null &&
                    typeof(IVocabularyPlugin).IsAssignableFrom(p.PluginType) &&
                    p.Name.Contains("欧路"));
            if (eudict != null)
            {
                AddService(eudict, ServiceType.Vocabulary);
                _logger.LogInformation("已自动添加默认生词本服务: 欧路词典");
            }
        }
    }

    /// <summary>
    /// 添加新的翻译服务实例
    /// </summary>
    /// <param name="metaData">插件元数据</param>
    /// <param name="type">服务类型</param>
    /// <returns>新创建的服务实例</returns>
    public Service AddService(PluginMetaData metaData, ServiceType type)
    {
        var service = CreateService(metaData);
        service.Initialize();
        _services.Add(service);

        var serviceDataCollection = GetServiceDataCollection(type);
        serviceDataCollection.Add(new ServiceData(service.ServiceID, service.DisplayName, service.IsEnabled));
        _serviceSettings.Save();
        return service;
    }

    /// <summary>
    /// 创建服务的独立副本，并复制其持久化配置。
    /// </summary>
    /// <param name="source">要复制的服务。</param>
    /// <param name="serviceType">服务类型。</param>
    /// <returns>复制成功时返回新服务；失败时返回 <see langword="null"/>。</returns>
    internal Service? DuplicateService(Service source, ServiceType serviceType)
    {
        var serviceDataCollection = GetServiceDataCollection(serviceType);
        var sourceDataIndex = serviceDataCollection.FindIndex(data => data.SvcID == source.ServiceID);
        if (sourceDataIndex < 0)
        {
            _logger.LogWarning(
                "无法复制 {ServiceType} 服务 {ServiceID}，因为未找到对应的服务配置。",
                serviceType,
                source.ServiceID);
            return null;
        }

        var duplicateServiceId = Guid.NewGuid().ToString("N");
        var duplicateData = CreateDuplicateServiceData(source, duplicateServiceId, serviceType);
        Service? duplicate = null;

        try
        {
            CopyPluginSettings(source, duplicateServiceId);
            duplicateData.IconPath = CopyCustomIcon(source, duplicateServiceId);

            duplicate = CreateService(source.MetaData, duplicateData);
            duplicate.Initialize();

            var sourceRuntimeIndex = _services.IndexOf(source);
            if (sourceRuntimeIndex >= 0)
                _services.Insert(sourceRuntimeIndex + 1, duplicate);
            else
                _services.Add(duplicate);

            serviceDataCollection.Insert(sourceDataIndex + 1, duplicateData);
            _serviceSettings.Save();
            return duplicate;
        }
        catch (Exception ex)
        {
            RollbackDuplicateService(duplicate, duplicateData, source.MetaData, serviceDataCollection);
            _logger.LogError(
                ex,
                "复制 {ServiceType} 服务 {ServiceID} 失败，已清理新服务 {DuplicateServiceID} 的数据。",
                serviceType,
                source.ServiceID,
                duplicateServiceId);
            return null;
        }
    }

    /// <summary>
    /// 移除服务实例
    /// </summary>
    /// <param name="service">要移除的服务实例</param>
    /// <param name="type">服务类型</param>
    public void RemoveService(Service service, ServiceType type)
    {
        // 清理服务自定义图标文件（不影响插件默认图标）
        if (!string.IsNullOrEmpty(service.IconPath) &&
            service.IconPath != service.MetaData.IconPath)
        {
            Helper.TryDeleteFile(service.IconPath);
        }

        service.Dispose();
        _services.Remove(service);

        var serviceDataCollection = GetServiceDataCollection(type);
        var serviceData = serviceDataCollection.FirstOrDefault(s => s.SvcID == service.ServiceID);

        if (serviceData != null)
        {
            serviceDataCollection.Remove(serviceData);
        }
    }

    private List<ServiceData> GetServiceDataCollection(ServiceType type)
    {
        return type switch
        {
            ServiceType.Translation => _serviceSettings.TranSvcDatas,
            ServiceType.OCR => _serviceSettings.OcrSvcDatas,
            ServiceType.TTS => _serviceSettings.TtsSvcDatas,
            ServiceType.Vocabulary => _serviceSettings.VocabularySvcDatas,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "不支持的服务类型")
        };
    }

    private static ServiceData CreateDuplicateServiceData(
        Service source,
        string duplicateServiceId,
        ServiceType serviceType) => new()
    {
        SvcID = duplicateServiceId,
        Name = $"{source.DisplayName}_New",
        // OCR、TTS 和生词本只允许单个服务启用，复制不能改变当前服务选择。
        IsEnabled = serviceType == ServiceType.Translation && source.IsEnabled,
        Options = source.Options == null
            ? null
            : new TranslationOptions
            {
                ExecMode = source.Options.ExecMode,
                AutoBackTranslation = source.Options.AutoBackTranslation
            }
    };

    private static void CopyPluginSettings(Service source, string duplicateServiceId)
    {
        var sourcePath = GetPluginSettingsPath(source.MetaData, source.ServiceID);
        var destinationPath = GetPluginSettingsPath(source.MetaData, duplicateServiceId);

        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("未找到待复制服务的插件配置文件。", sourcePath);

        Directory.CreateDirectory(source.MetaData.PluginSettingsDirectoryPath);
        File.Copy(sourcePath, destinationPath);
    }

    private static string? CopyCustomIcon(Service source, string duplicateServiceId)
    {
        if (string.IsNullOrEmpty(source.IconPath) || source.IconPath == source.MetaData.IconPath)
            return null;

        var iconsDirectory = Path.Combine(source.MetaData.PluginSettingsDirectoryPath, "icons");
        var destinationPath = Path.Combine(iconsDirectory, $"{duplicateServiceId}{Path.GetExtension(source.IconPath)}");

        Directory.CreateDirectory(iconsDirectory);
        File.Copy(source.IconPath, destinationPath);
        return Helper.ToRelativeIconPath(destinationPath, source.MetaData.PluginSettingsDirectoryPath);
    }

    private void RollbackDuplicateService(
        Service? duplicate,
        ServiceData duplicateData,
        PluginMetaData metaData,
        List<ServiceData> serviceDataCollection)
    {
        if (duplicate != null)
        {
            _services.Remove(duplicate);
            try
            {
                if (duplicate.IsInitialized)
                    duplicate.Dispose();
                else
                    duplicate.Plugin.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "释放未完成复制的服务 {ServiceID} 时发生异常。", duplicate.ServiceID);
            }
        }

        serviceDataCollection.Remove(duplicateData);

        var settingsPath = GetPluginSettingsPath(metaData, duplicateData.SvcID);
        Helper.TryDeleteFile(settingsPath);
        Helper.TryDeleteFile($"{settingsPath}.bak");
        Helper.TryDeleteFile($"{settingsPath}.tmp");

        if (string.IsNullOrEmpty(duplicateData.IconPath))
            return;

        var iconPath = Helper.ToAbsoluteIconPath(duplicateData.IconPath, metaData.PluginSettingsDirectoryPath);
        Helper.TryDeleteFile(iconPath);

        var iconsDirectory = Path.GetDirectoryName(iconPath);
        if (!string.IsNullOrEmpty(iconsDirectory) &&
            Directory.Exists(iconsDirectory) &&
            !Directory.EnumerateFileSystemEntries(iconsDirectory).Any())
        {
            Helper.TryDeleteDirectory(iconsDirectory);
        }
    }

    private static string GetPluginSettingsPath(PluginMetaData metaData, string serviceId) =>
        Path.Combine(metaData.PluginSettingsDirectoryPath, $"{serviceId}{StorageBase<ServiceSettings>.FileSuffix}");

    private Service CreateService(PluginMetaData metaData, ServiceData? settings = null)
    {
        var metaDataClone = metaData.Clone();
        var serviceID = settings?.SvcID ?? Guid.NewGuid().ToString("N");

        var service = new Service
        {
            ServiceID = serviceID,
            MetaData = metaDataClone,
            IsEnabled = settings?.IsEnabled ?? false,
            DisplayName = settings?.Name ?? metaDataClone.Name,
            IconPath = string.IsNullOrEmpty(settings?.IconPath)
                ? string.Empty
                : Helper.ToAbsoluteIconPath(settings.IconPath, metaDataClone.PluginSettingsDirectoryPath)
        };

        // 针对翻译/词典插件，设置执行模式和自动回译选项尝试从缓存加载
        if (metaData.PluginType != null &&
            (typeof(ITranslatePlugin).IsAssignableFrom(metaData.PluginType) ||
                typeof(IDictionaryPlugin).IsAssignableFrom(metaData.PluginType)))
        {
            service.Options ??= new TranslationOptions();
            service.Options.ExecMode = settings?.Options?.ExecMode ?? ExecutionMode.Automatic;
            service.Options.AutoBackTranslation = settings?.Options?.AutoBackTranslation ?? false;
        }
        var plugin = metaDataClone.CreatePluginService();
        var context = new PluginContext(metaDataClone, serviceID);
        service.Plugin = plugin;
        service.Context = context;

        return service;
    }

    /// <summary>
    /// 尝试删除与丢失的服务关联的配置文件和目录。
    /// </summary>
    /// <remarks>
    /// 此方法会移除以指定服务标识命名的配置文件，并在其父目录变为空时将其删除。文件或目录删除过程中遇到的错误会被记录下来，但不会中止清理过程。
    /// </remarks>
    /// <param name="lossSvcs">这是一组服务标识符，用于代表那些需要清理配置文件的服务。每个标识符都用于定位并删除相应的文件。</param>
    private void TryCleanupLostServiceFiles(IEnumerable<string> lossSvcs)
    {
        foreach (var svcId in lossSvcs)
        {
            var directories = Directory.EnumerateDirectories(DataLocation.PluginSettingsDirectory);
            foreach (var dir in directories)
            {
                var filePath = Path.Combine(dir, $"{svcId}.json");
                if (File.Exists(filePath))
                {
                    try
                    {
                        File.Delete(filePath);
                        _logger.LogInformation($"已删除丢失服务的配置文件: {filePath}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"删除丢失服务配置文件失败: {filePath}");
                    }
                    // 检查目录是否为空，若为空则删除该目录
                    if (!Directory.EnumerateFileSystemEntries(dir).Any())
                    {
                        try
                        {
                            Directory.Delete(dir);
                            _logger.LogInformation($"已删除空的服务配置目录: {dir}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"删除空服务配置目录失败: {dir}");
                        }
                    }
                }
            }
        }
    }
}
