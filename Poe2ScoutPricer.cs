// Poe2ScoutPricer.cs
using System.Diagnostics;
using ExileCore2;
using ExileCore2.PoEMemory;
using ExileCore2.PoEMemory.Models;
using Poe2ScoutPricer.API;
using Poe2ScoutPricer.Configuration;
using Poe2ScoutPricer.Models;
using Poe2ScoutPricer.Services;
using Poe2ScoutPricer.UI;
using Poe2ScoutPricer.Utils;

namespace Poe2ScoutPricer
{
    public class Poe2ScoutPricer : BaseSettingsPlugin<Poe2ScoutSettings>
    {
        // Services
        private IPoe2ScoutApi? _apiClient;
        private ICacheService? _cacheService;
        private IItemMatcher? _itemMatcher;
        private IPriceService? _priceService;
        
        // UI Renderers
        private HoveredItemRenderer? _hoveredItemRenderer;
        private InventoryRenderer? _inventoryRenderer;
        private GroundItemRenderer? _groundItemRenderer;
        
        // Timers and state
        private readonly Stopwatch _updateTimer = Stopwatch.StartNew();
        private readonly Stopwatch _dataReloadTimer = Stopwatch.StartNew();
        private CustomItem? _hoveredItem;
        private CustomItem? _inspectedItem;
        private string _currentLeague = "Standard";
        private bool _isInitialized = false;
        private bool _dataLoadInProgress = false;

        public override bool Initialise()
        {
            try
            {
                Logger.LogAction = LogMessage;
                Logger.DebugEnabled = Settings.DebugSettings.EnableDebugLogging;
                
                Logger.LogInfo("Initializing Poe2ScoutPricer plugin");
                
                // Set GameController reference
                CustomItem.GameController.Instance = GameController;
                
                // Initialize services
                InitializeServices();
                
                // Setup event handlers
                SetupEventHandlers();
                
                // Initialize UI renderers
                InitializeRenderers();
                
                // Load initial data
                _ = Task.Run(async () => await LoadInitialDataAsync());
                
                // Register plugin bridge methods
                RegisterPluginBridgeMethods();
                
                _isInitialized = true;
                Logger.LogInfo("Poe2ScoutPricer plugin initialized successfully");
                
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to initialize plugin: {ex.Message}");
                LogError($"Plugin initialization failed: {ex}");
                return false;
            }
        }

        private void InitializeServices()
        {
            _cacheService = new CacheService();
            _apiClient = new Poe2ScoutApiClient();
            _itemMatcher = new ItemMatcher();
            _priceService = new PriceService(_apiClient, _cacheService, _itemMatcher);
            
            Logger.LogInfo("Services initialized");
        }

        private void SetupEventHandlers()
        {
            // Settings event handlers
            Settings.ApiSettings.ReloadPrices.OnPressed += () => _ = Task.Run(ReloadPricesAsync);
            Settings.ApiSettings.ClearCache.OnPressed += ClearCache;
            Settings.DebugSettings.ResetInspectedItem.OnPressed += () => _inspectedItem = null;
            Settings.DebugSettings.EnableDebugLogging.OnValueChanged += (_, value) => Logger.DebugEnabled = value;
            Settings.ApiSettings.SyncCurrentLeague.OnValueChanged += (_, _) => SyncCurrentLeague();
            
            Logger.LogInfo("Event handlers set up");
        }

        private void InitializeRenderers()
        {
            if (_priceService == null) return;
            
            _hoveredItemRenderer = new HoveredItemRenderer(Settings, _priceService);
            _inventoryRenderer = new InventoryRenderer(Settings, _priceService);
            _groundItemRenderer = new GroundItemRenderer(Settings, _priceService);
            
            Logger.LogInfo("UI renderers initialized");
        }

        private void RegisterPluginBridgeMethods()
        {
            // Register methods for other plugins to use
            GameController.PluginBridge.SaveMethod("Poe2ScoutPricer.GetPrice", (Entity entity) =>
            {
                if (_priceService == null) return 0.0;
                
                var customItem = new CustomItem(entity);
                var priceTask = _priceService.GetItemPriceAsync(customItem, _currentLeague);
                priceTask.Wait(1000); // Wait max 1 second
                
                return priceTask.IsCompletedSuccessfully ? priceTask.Result.GetBestPrice() : 0.0;
            });
            
            GameController.PluginBridge.SaveMethod("Poe2ScoutPricer.IsDataLoaded", () => 
                _priceService?.IsDataLoaded ?? false);
            
            Logger.LogInfo("Plugin bridge methods registered");
        }

        public override void AreaChange(AreaInstance area)
        {
            _hoveredItem = null;
            _inspectedItem = null;
            
            SyncCurrentLeague();
            
            Logger.LogInfo($"Area changed to: {area.DisplayName}");
        }

        public override void Render()
        {
            try
            {
                if (!_isInitialized || _priceService == null) 
                    return;

                // Update hovered item
                UpdateHoveredItem();
                
                // Handle inspected item
                if (_inspectedItem != null)
                {
                    GameController.InspectObject(_inspectedItem, "Poe2Scout Inspected Item");
                }
                
                // Render UI elements
                RenderUI();
                
                // Check for data reload
                CheckDataReload();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Render error: {ex.Message}");
            }
        }

        private void UpdateHoveredItem()
        {
            if (!Settings.HoveredItemSettings.ShowHoveredItemPrices || _priceService == null)
            {
                _hoveredItem = null;
                return;
            }

            try
            {
                var uiHover = GameController.Game.IngameState.UIHover;
                if (uiHover?.AsObject<ExileCore2.PoEMemory.Elements.InventoryElements.NormalInventoryItem>() is var inventoryItem &&
                    inventoryItem?.Item?.IsValid == true)
                {
                    var item = new CustomItem(inventoryItem);
                    
                    if (item.ItemType != ItemTypes.None)
                    {
                        _hoveredItem = item;
                        
                        // Handle inspect hotkey
                        if (Settings.DebugSettings.InspectHoverHotkey.PressedOnce())
                        {
                            _inspectedItem = item;
                        }
                        
                        // Get price asynchronously
                        if (!item.HasValidPrice && _updateTimer.ElapsedMilliseconds > Settings.UpdateSettings.UpdateCheckIntervalMs)
                        {
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    item.PriceData = await _priceService.GetItemPriceAsync(item, _currentLeague);
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogError($"Error getting price for hovered item: {ex.Message}");
                                }
                            });
                            
                            _updateTimer.Restart();
                        }
                    }
                }
                else
                {
                    _hoveredItem = null;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error updating hovered item: {ex.Message}");
                _hoveredItem = null;
            }
        }

        private void RenderUI()
        {
            // Render hovered item tooltip
            if (_hoveredItem != null && Settings.HoveredItemSettings.ShowHoveredItemPrices)
            {
                _hoveredItemRenderer?.Render(_hoveredItem);
            }
            
            // Render inventory value
            if (Settings.InventorySettings.ShowInventoryValue)
            {
                _inventoryRenderer?.Render();
            }
            
            // Render ground item prices
            if (Settings.GroundItemSettings.ShowGroundItemPrices)
            {
                _groundItemRenderer?.Render();
            }
        }

        private void CheckDataReload()
        {
            if (!Settings.UpdateSettings.AutoReload || _dataLoadInProgress) 
                return;
            
            var reloadInterval = TimeSpan.FromMinutes(Settings.UpdateSettings.ReloadIntervalMinutes);
            if (_dataReloadTimer.Elapsed > reloadInterval)
            {
                _ = Task.Run(ReloadPricesAsync);
                _dataReloadTimer.Restart();
            }
        }

        private async Task LoadInitialDataAsync()
        {
            if (_priceService == null || _dataLoadInProgress) 
                return;
            
            _dataLoadInProgress = true;
            
            try
            {
                Logger.LogInfo("Loading initial price data");
                
                // Load leagues first
                await LoadLeaguesAsync();
                
                // Load price data
                var success = await _priceService.LoadAllDataAsync(_currentLeague);
                if (success)
                {
                    Logger.LogInfo("Initial data loaded successfully");
                }
                else
                {
                    Logger.LogError("Failed to load initial data");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error loading initial data: {ex.Message}");
            }
            finally
            {
                _dataLoadInProgress = false;
            }
        }

        private async Task LoadLeaguesAsync()
        {
            if (_apiClient == null) return;
            
            try
            {
                var response = await _apiClient.GetLeaguesAsync();
                if (response.IsSuccess && response.Data?.Any() == true)
                {
                    var leagues = response.Data.Select(l => l.Value).ToList();
                    Settings.ApiSettings.League.Values = leagues;
                    
                    if (Settings.ApiSettings.League.Values.Contains("Standard") && 
                        string.IsNullOrEmpty(Settings.ApiSettings.League.Value))
                    {
                        Settings.ApiSettings.League.Value = "Standard";
                    }
                    
                    Logger.LogInfo($"Loaded {leagues.Count} leagues");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error loading leagues: {ex.Message}");
            }
        }

        private async Task ReloadPricesAsync()
        {
            if (_priceService == null || _dataLoadInProgress) 
                return;
            
            _dataLoadInProgress = true;
            
            try
            {
                Logger.LogInfo("Reloading price data");
                
                var success = await _priceService.RefreshDataAsync(_currentLeague);
                if (success)
                {
                    Logger.LogInfo("Price data reloaded successfully");
                }
                else
                {
                    Logger.LogError("Failed to reload price data");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error reloading prices: {ex.Message}");
            }
            finally
            {
                _dataLoadInProgress = false;
            }
        }

        private void ClearCache()
        {
            _cacheService?.Clear();
            Logger.LogInfo("Cache cleared");
        }

        private void SyncCurrentLeague()
        {
            if (!Settings.ApiSettings.SyncCurrentLeague) 
                return;
            
            try
            {
                var currentLeague = GameController.Game.IngameState.ServerData.League;
                if (!string.IsNullOrEmpty(currentLeague) && currentLeague != _currentLeague)
                {
                    _currentLeague = currentLeague;
                    
                    if (!Settings.ApiSettings.League.Values.Contains(currentLeague))
                    {
                        Settings.ApiSettings.League.Values.Add(currentLeague);
                    }
                    
                    Settings.ApiSettings.League.Value = currentLeague;
                    
                    Logger.LogInfo($"Synced to current league: {currentLeague}");
                    
                    // Reload data for new league
                    _ = Task.Run(ReloadPricesAsync);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error syncing current league: {ex.Message}");
            }
        }

        public override void Dispose()
        {
            try
            {
                Logger.LogInfo("Disposing Poe2ScoutPricer plugin");
                
                _apiClient?.Dispose();
                _cacheService?.Dispose();
                _hoveredItemRenderer?.Dispose();
                _inventoryRenderer?.Dispose();
                _groundItemRenderer?.Dispose();
                
                Logger.LogInfo("Plugin disposed successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during disposal: {ex.Message}");
            }
            
            base.Dispose();
        }
    }
}