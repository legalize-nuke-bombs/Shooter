* Поправить ошибки билда
* Почему-то простой туман играется лучше, чем объемный. Наверное стоить заменить весь туман на простой
* Мб попробовать более облачное небо
* Отражения
* Оптимизация



Приложение, логи билда:
```
1 HDRP assets included in build- HDRenderPipelineAsset - Assets/Shooter/Data/Settings/HDRenderPipelineAsset.asset



UnityEditor.EditorApplication:Internal\_CallDelayFunctions ()



Building with multiplayer role: ClientAndServer

UnityEngine.Debug:Log (object)

Unity.Multiplayer.Editor.MultiplayerRolesBuildPreprocessor:OnPreprocessBuild (UnityEditor.Build.Reporting.BuildReport)

UnityEditor.EditorApplication:Internal\_CallDelayFunctions ()



Catalog: Catalog TipCatalog knows 12 things

UnityEngine.Logger:Log (UnityEngine.LogType,object)

Shooter.Infrastructure.Logs:WriteToUnityLogger (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:WriteAtLevel (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:Info (string)

Shooter.Infrastructure.Catalog`1<Shooter.Game.LoadingScreenTipAsset>:OnEnable () (at Assets/Shooter/Scripts/Infrastructure/Catalogs/Catalog.cs:52)

UnityEditor.AssetDatabase:LoadAssetAtPath<Shooter.Infrastructure.Catalog> (string)

Shooter.Editor.CatalogRefillPostprocessor:OnPostprocessAllAssets (string\[],string\[],string\[],string\[]) (at Assets/Shooter/Scripts/Editor/AssetPostprocessors/CatalogRefillPostprocessor.cs:22)

UnityEditor.EditorApplication:Internal\_CallDelayFunctions ()



Catalog: Catalog EarSoundCatalog knows 13 things

UnityEngine.Logger:Log (UnityEngine.LogType,object)

Shooter.Infrastructure.Logs:WriteToUnityLogger (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:WriteAtLevel (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:Info (string)

Shooter.Infrastructure.Catalog`1<Shooter.Game.EarSoundAsset>:OnEnable () (at Assets/Shooter/Scripts/Infrastructure/Catalogs/Catalog.cs:52)

UnityEditor.AssetDatabase:LoadAssetAtPath<Shooter.Infrastructure.Catalog> (string)

Shooter.Editor.CatalogRefillPostprocessor:OnPostprocessAllAssets (string\[],string\[],string\[],string\[]) (at Assets/Shooter/Scripts/Editor/AssetPostprocessors/CatalogRefillPostprocessor.cs:22)

UnityEditor.EditorApplication:Internal\_CallDelayFunctions ()



Catalog: Catalog FactionCatalog knows 4 things

UnityEngine.Logger:Log (UnityEngine.LogType,object)

Shooter.Infrastructure.Logs:WriteToUnityLogger (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:WriteAtLevel (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:Info (string)

Shooter.Infrastructure.Catalog`1<Shooter.Game.Faction>:OnEnable () (at Assets/Shooter/Scripts/Infrastructure/Catalogs/Catalog.cs:52)

UnityEditor.AssetDatabase:LoadAssetAtPath<Shooter.Infrastructure.Catalog> (string)

Shooter.Editor.CatalogRefillPostprocessor:OnPostprocessAllAssets (string\[],string\[],string\[],string\[]) (at Assets/Shooter/Scripts/Editor/AssetPostprocessors/CatalogRefillPostprocessor.cs:22)

UnityEditor.EditorApplication:Internal\_CallDelayFunctions ()



Catalog: Catalog FactionRelationCatalog knows 10 things

UnityEngine.Logger:Log (UnityEngine.LogType,object)

Shooter.Infrastructure.Logs:WriteToUnityLogger (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:WriteAtLevel (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:Info (string)

Shooter.Infrastructure.Catalog`1<Shooter.Game.FactionRelation>:OnEnable () (at Assets/Shooter/Scripts/Infrastructure/Catalogs/Catalog.cs:52)

Shooter.Game.FactionRelationCatalog:OnEnable () (at Assets/Shooter/Scripts/Game/Relations/FactionRelationCatalog.cs:18)

UnityEditor.AssetDatabase:LoadAssetAtPath<Shooter.Infrastructure.Catalog> (string)

Shooter.Editor.CatalogRefillPostprocessor:OnPostprocessAllAssets (string\[],string\[],string\[],string\[]) (at Assets/Shooter/Scripts/Editor/AssetPostprocessors/CatalogRefillPostprocessor.cs:22)

UnityEditor.EditorApplication:Internal\_CallDelayFunctions ()



Catalog: Catalog IconCatalog knows 21 things

UnityEngine.Logger:Log (UnityEngine.LogType,object)

Shooter.Infrastructure.Logs:WriteToUnityLogger (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:WriteAtLevel (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:Info (string)

Shooter.Infrastructure.Catalog`1<Shooter.Game.IconAsset>:OnEnable () (at Assets/Shooter/Scripts/Infrastructure/Catalogs/Catalog.cs:52)

UnityEditor.AssetDatabase:LoadAssetAtPath<Shooter.Infrastructure.Catalog> (string)

Shooter.Editor.CatalogRefillPostprocessor:OnPostprocessAllAssets (string\[],string\[],string\[],string\[]) (at Assets/Shooter/Scripts/Editor/AssetPostprocessors/CatalogRefillPostprocessor.cs:22)

UnityEditor.EditorApplication:Internal\_CallDelayFunctions ()



Catalog: Catalog ItemCatalog knows 14 things

UnityEngine.Logger:Log (UnityEngine.LogType,object)

Shooter.Infrastructure.Logs:WriteToUnityLogger (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:WriteAtLevel (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:Info (string)

Shooter.Infrastructure.Catalog`1<Shooter.Game.ItemAsset>:OnEnable () (at Assets/Shooter/Scripts/Infrastructure/Catalogs/Catalog.cs:52)

UnityEditor.AssetDatabase:LoadAssetAtPath<Shooter.Infrastructure.Catalog> (string)

Shooter.Editor.CatalogRefillPostprocessor:OnPostprocessAllAssets (string\[],string\[],string\[],string\[]) (at Assets/Shooter/Scripts/Editor/AssetPostprocessors/CatalogRefillPostprocessor.cs:22)

UnityEditor.EditorApplication:Internal\_CallDelayFunctions ()



Catalog: Catalog NameCatalog knows 14 things

UnityEngine.Logger:Log (UnityEngine.LogType,object)

Shooter.Infrastructure.Logs:WriteToUnityLogger (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:WriteAtLevel (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:Info (string)

Shooter.Infrastructure.Catalog`1<Shooter.Game.NameAsset>:OnEnable () (at Assets/Shooter/Scripts/Infrastructure/Catalogs/Catalog.cs:52)

UnityEditor.AssetDatabase:LoadAssetAtPath<Shooter.Infrastructure.Catalog> (string)

Shooter.Editor.CatalogRefillPostprocessor:OnPostprocessAllAssets (string\[],string\[],string\[],string\[]) (at Assets/Shooter/Scripts/Editor/AssetPostprocessors/CatalogRefillPostprocessor.cs:22)

UnityEditor.EditorApplication:Internal\_CallDelayFunctions ()



Catalog: Catalog NotificationCatalog knows 3 things

UnityEngine.Logger:Log (UnityEngine.LogType,object)

Shooter.Infrastructure.Logs:WriteToUnityLogger (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:WriteAtLevel (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:Info (string)

Shooter.Infrastructure.Catalog`1<Shooter.Game.NotificationAsset>:OnEnable () (at Assets/Shooter/Scripts/Infrastructure/Catalogs/Catalog.cs:52)

UnityEditor.AssetDatabase:LoadAssetAtPath<Shooter.Infrastructure.Catalog> (string)

Shooter.Editor.CatalogRefillPostprocessor:OnPostprocessAllAssets (string\[],string\[],string\[],string\[]) (at Assets/Shooter/Scripts/Editor/AssetPostprocessors/CatalogRefillPostprocessor.cs:22)

UnityEditor.EditorApplication:Internal\_CallDelayFunctions ()



Catalog: Catalog SaveablePrefabCatalog knows 3 things

UnityEngine.Logger:Log (UnityEngine.LogType,object)

Shooter.Infrastructure.Logs:WriteToUnityLogger (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:WriteAtLevel (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:Info (string)

Shooter.Infrastructure.Catalog`1<Shooter.Infrastructure.SaveablePrefabAsset>:OnEnable () (at Assets/Shooter/Scripts/Infrastructure/Catalogs/Catalog.cs:52)

Shooter.Infrastructure.SaveablePrefabCatalog:OnEnable () (at Assets/Shooter/Scripts/Infrastructure/Saves/Snapshot/Prefabs/SaveablePrefabCatalog.cs:17)

UnityEditor.AssetDatabase:LoadAssetAtPath<Shooter.Infrastructure.Catalog> (string)

Shooter.Editor.CatalogRefillPostprocessor:OnPostprocessAllAssets (string\[],string\[],string\[],string\[]) (at Assets/Shooter/Scripts/Editor/AssetPostprocessors/CatalogRefillPostprocessor.cs:22)

UnityEditor.EditorApplication:Internal\_CallDelayFunctions ()



Catalog: Catalog Skins knows 32 things

UnityEngine.Logger:Log (UnityEngine.LogType,object)

Shooter.Infrastructure.Logs:WriteToUnityLogger (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:WriteAtLevel (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:Info (string)

Shooter.Infrastructure.Catalog`1<Shooter.Game.SkinAsset>:OnEnable () (at Assets/Shooter/Scripts/Infrastructure/Catalogs/Catalog.cs:52)

UnityEditor.AssetDatabase:LoadAssetAtPath<Shooter.Infrastructure.Catalog> (string)

Shooter.Editor.CatalogRefillPostprocessor:OnPostprocessAllAssets (string\[],string\[],string\[],string\[]) (at Assets/Shooter/Scripts/Editor/AssetPostprocessors/CatalogRefillPostprocessor.cs:22)

UnityEditor.EditorApplication:Internal\_CallDelayFunctions ()



Catalog: Catalog SoundCatalog knows 34 things

UnityEngine.Logger:Log (UnityEngine.LogType,object)

Shooter.Infrastructure.Logs:WriteToUnityLogger (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:WriteAtLevel (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:Info (string)

Shooter.Infrastructure.Catalog`1<Shooter.Game.SoundAsset>:OnEnable () (at Assets/Shooter/Scripts/Infrastructure/Catalogs/Catalog.cs:52)

UnityEditor.AssetDatabase:LoadAssetAtPath<Shooter.Infrastructure.Catalog> (string)

Shooter.Editor.CatalogRefillPostprocessor:OnPostprocessAllAssets (string\[],string\[],string\[],string\[]) (at Assets/Shooter/Scripts/Editor/AssetPostprocessors/CatalogRefillPostprocessor.cs:22)

UnityEditor.EditorApplication:Internal\_CallDelayFunctions ()



Catalog: Catalog ToxinCatalog knows 2 things

UnityEngine.Logger:Log (UnityEngine.LogType,object)

Shooter.Infrastructure.Logs:WriteToUnityLogger (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:WriteAtLevel (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:Info (string)

Shooter.Infrastructure.Catalog`1<Shooter.Game.ToxinAsset>:OnEnable () (at Assets/Shooter/Scripts/Infrastructure/Catalogs/Catalog.cs:52)

UnityEditor.AssetDatabase:LoadAssetAtPath<Shooter.Infrastructure.Catalog> (string)

Shooter.Editor.CatalogRefillPostprocessor:OnPostprocessAllAssets (string\[],string\[],string\[],string\[]) (at Assets/Shooter/Scripts/Editor/AssetPostprocessors/CatalogRefillPostprocessor.cs:22)

UnityEditor.EditorApplication:Internal\_CallDelayFunctions ()



Pipeline: No RuntimePipelineManager components found in build scenes. Pipeline will be disabled in Player builds.

UnityEngine.Debug:LogWarning (object)

Unity.Pipeline.Editor.BuildProcessors.PipelineRuntimeBuildProcessor:OnPreprocessBuild (UnityEditor.Build.BuildCallbackContext) (at ./Library/PackageCache/com.unity.pipeline@24975d51528a/Editor/BuildProcessors/PipelineRuntimeBuildProcessor.cs:52)

UnityEditor.EditorApplication:Internal\_CallDelayFunctions ()



Catalog: Catalog EarSoundCatalog knows 13 things

UnityEngine.Logger:Log (UnityEngine.LogType,object)

Shooter.Infrastructure.Logs:WriteToUnityLogger (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:WriteAtLevel (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:Info (string)

Shooter.Infrastructure.Catalog`1<Shooter.Game.EarSoundAsset>:OnEnable () (at Assets/Shooter/Scripts/Infrastructure/Catalogs/Catalog.cs:52)

UnityEditor.EditorApplication:Internal\_CallDelayFunctions ()



Catalog: Catalog FactionCatalog knows 4 things

UnityEngine.Logger:Log (UnityEngine.LogType,object)

Shooter.Infrastructure.Logs:WriteToUnityLogger (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:WriteAtLevel (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:Info (string)

Shooter.Infrastructure.Catalog`1<Shooter.Game.Faction>:OnEnable () (at Assets/Shooter/Scripts/Infrastructure/Catalogs/Catalog.cs:52)

UnityEditor.EditorApplication:Internal\_CallDelayFunctions ()



Catalog: Catalog FactionRelationCatalog knows 10 things

UnityEngine.Logger:Log (UnityEngine.LogType,object)

Shooter.Infrastructure.Logs:WriteToUnityLogger (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:WriteAtLevel (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:Info (string)

Shooter.Infrastructure.Catalog`1<Shooter.Game.FactionRelation>:OnEnable () (at Assets/Shooter/Scripts/Infrastructure/Catalogs/Catalog.cs:52)

Shooter.Game.FactionRelationCatalog:OnEnable () (at Assets/Shooter/Scripts/Game/Relations/FactionRelationCatalog.cs:18)

UnityEditor.EditorApplication:Internal\_CallDelayFunctions ()



Catalog: Catalog IconCatalog knows 21 things

UnityEngine.Logger:Log (UnityEngine.LogType,object)

Shooter.Infrastructure.Logs:WriteToUnityLogger (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:WriteAtLevel (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:Info (string)

Shooter.Infrastructure.Catalog`1<Shooter.Game.IconAsset>:OnEnable () (at Assets/Shooter/Scripts/Infrastructure/Catalogs/Catalog.cs:52)

UnityEditor.EditorApplication:Internal\_CallDelayFunctions ()



Catalog: Catalog ItemCatalog knows 14 things

UnityEngine.Logger:Log (UnityEngine.LogType,object)

Shooter.Infrastructure.Logs:WriteToUnityLogger (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:WriteAtLevel (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:Info (string)

Shooter.Infrastructure.Catalog`1<Shooter.Game.ItemAsset>:OnEnable () (at Assets/Shooter/Scripts/Infrastructure/Catalogs/Catalog.cs:52)

UnityEditor.EditorApplication:Internal\_CallDelayFunctions ()



Catalog: Catalog NameCatalog knows 14 things

UnityEngine.Logger:Log (UnityEngine.LogType,object)

Shooter.Infrastructure.Logs:WriteToUnityLogger (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:WriteAtLevel (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:Info (string)

Shooter.Infrastructure.Catalog`1<Shooter.Game.NameAsset>:OnEnable () (at Assets/Shooter/Scripts/Infrastructure/Catalogs/Catalog.cs:52)

UnityEditor.EditorApplication:Internal\_CallDelayFunctions ()



Catalog: Catalog NotificationCatalog knows 3 things

UnityEngine.Logger:Log (UnityEngine.LogType,object)

Shooter.Infrastructure.Logs:WriteToUnityLogger (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:WriteAtLevel (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:Info (string)

Shooter.Infrastructure.Catalog`1<Shooter.Game.NotificationAsset>:OnEnable () (at Assets/Shooter/Scripts/Infrastructure/Catalogs/Catalog.cs:52)

UnityEditor.EditorApplication:Internal\_CallDelayFunctions ()



Catalog: Catalog SaveablePrefabCatalog knows 3 things

UnityEngine.Logger:Log (UnityEngine.LogType,object)

Shooter.Infrastructure.Logs:WriteToUnityLogger (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:WriteAtLevel (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:Info (string)

Shooter.Infrastructure.Catalog`1<Shooter.Infrastructure.SaveablePrefabAsset>:OnEnable () (at Assets/Shooter/Scripts/Infrastructure/Catalogs/Catalog.cs:52)

Shooter.Infrastructure.SaveablePrefabCatalog:OnEnable () (at Assets/Shooter/Scripts/Infrastructure/Saves/Snapshot/Prefabs/SaveablePrefabCatalog.cs:17)

UnityEditor.EditorApplication:Internal\_CallDelayFunctions ()



Catalog: Catalog Skins knows 32 things

UnityEngine.Logger:Log (UnityEngine.LogType,object)

Shooter.Infrastructure.Logs:WriteToUnityLogger (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:WriteAtLevel (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:Info (string)

Shooter.Infrastructure.Catalog`1<Shooter.Game.SkinAsset>:OnEnable () (at Assets/Shooter/Scripts/Infrastructure/Catalogs/Catalog.cs:52)

UnityEditor.EditorApplication:Internal\_CallDelayFunctions ()



Catalog: Catalog SoundCatalog knows 34 things

UnityEngine.Logger:Log (UnityEngine.LogType,object)

Shooter.Infrastructure.Logs:WriteToUnityLogger (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:WriteAtLevel (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:Info (string)

Shooter.Infrastructure.Catalog`1<Shooter.Game.SoundAsset>:OnEnable () (at Assets/Shooter/Scripts/Infrastructure/Catalogs/Catalog.cs:52)

UnityEditor.EditorApplication:Internal\_CallDelayFunctions ()



Catalog: Catalog ToxinCatalog knows 2 things

UnityEngine.Logger:Log (UnityEngine.LogType,object)

Shooter.Infrastructure.Logs:WriteToUnityLogger (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:WriteAtLevel (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:Info (string)

Shooter.Infrastructure.Catalog`1<Shooter.Game.ToxinAsset>:OnEnable () (at Assets/Shooter/Scripts/Infrastructure/Catalogs/Catalog.cs:52)

UnityEditor.EditorApplication:Internal\_CallDelayFunctions ()



NullReferenceException: Object reference not set to an instance of an object

UnityEngine.Rendering.ProbeReferenceVolume.UpdateActiveRequests (UnityEngine.Rendering.CommandBuffer cmd) (at Library/PackageCache/com.unity.render-pipelines.core@77e675a23c5f/Runtime/Lighting/ProbeVolume/ProbeReferenceVolume.Streaming.cs:1335)

UnityEngine.Rendering.ProbeReferenceVolume.CleanupStreaming () (at Library/PackageCache/com.unity.render-pipelines.core@77e675a23c5f/Runtime/Lighting/ProbeVolume/ProbeReferenceVolume.Streaming.cs:410)

UnityEngine.Rendering.ProbeReferenceVolume.Cleanup () (at Library/PackageCache/com.unity.render-pipelines.core@77e675a23c5f/Runtime/Lighting/ProbeVolume/ProbeReferenceVolume.cs:1162)

UnityEngine.Rendering.HighDefinition.HDRenderPipeline.Dispose (System.Boolean disposing) (at Library/PackageCache/com.unity.render-pipelines.high-definition@6fc877b69258/Runtime/RenderPipeline/HDRenderPipeline.cs:1097)

UnityEngine.Rendering.RenderPipeline.Dispose () (at <6a3be24f609f4c7e856c987d6a26bd21>:0)

UnityEngine.Rendering.RenderPipelineManager.CleanupRenderPipeline () (at <6a3be24f609f4c7e856c987d6a26bd21>:0)

UnityEngine.Rendering.RenderPipelineManager.HandleRenderPipelineChange (UnityEngine.Rendering.RenderPipelineAsset pipelineAsset) (at <6a3be24f609f4c7e856c987d6a26bd21>:0)

UnityEditor.EditorApplication:Internal\_CallDelayFunctions()



NullReferenceException: Object reference not set to an instance of an object

UnityEngine.Rendering.HighDefinition.HDRenderPipeline.DisposeProbeCameraPool () (at Library/PackageCache/com.unity.render-pipelines.high-definition@6fc877b69258/Runtime/RenderPipeline/HDRenderPipeline.cs:963)

UnityEngine.Rendering.HighDefinition.HDRenderPipeline.Dispose (System.Boolean disposing) (at Library/PackageCache/com.unity.render-pipelines.high-definition@6fc877b69258/Runtime/RenderPipeline/HDRenderPipeline.cs:978)

UnityEngine.Rendering.RenderPipeline.Dispose () (at <6a3be24f609f4c7e856c987d6a26bd21>:0)

UnityEngine.Rendering.RenderPipelineManager.CleanupRenderPipeline () (at <6a3be24f609f4c7e856c987d6a26bd21>:0)

UnityEngine.Rendering.RenderPipelineManager.HandleRenderPipelineChange (UnityEngine.Rendering.RenderPipelineAsset pipelineAsset) (at <6a3be24f609f4c7e856c987d6a26bd21>:0)

UnityEngine.Rendering.RenderPipelineManager.TryPrepareRenderPipeline (UnityEngine.Rendering.RenderPipelineAsset pipelineAsset) (at <6a3be24f609f4c7e856c987d6a26bd21>:0)

UnityEngine.Rendering.RenderPipelineManager.DoRenderLoop\_Internal (UnityEngine.Rendering.RenderPipelineAsset pipelineAsset, System.IntPtr loopPtr, System.Object renderRequest, Unity.Collections.LowLevel.Unsafe.AtomicSafetyHandle safety) (at <6a3be24f609f4c7e856c987d6a26bd21>:0)

UnityEditor.EditorApplication:Internal\_CallDelayFunctions()



NullReferenceException: Object reference not set to an instance of an object

UnityEngine.Rendering.HighDefinition.HDRenderPipeline.DisposeProbeCameraPool () (at Library/PackageCache/com.unity.render-pipelines.high-definition@6fc877b69258/Runtime/RenderPipeline/HDRenderPipeline.cs:963)

UnityEngine.Rendering.HighDefinition.HDRenderPipeline.Dispose (System.Boolean disposing) (at Library/PackageCache/com.unity.render-pipelines.high-definition@6fc877b69258/Runtime/RenderPipeline/HDRenderPipeline.cs:978)

UnityEngine.Rendering.RenderPipeline.Dispose () (at <6a3be24f609f4c7e856c987d6a26bd21>:0)

UnityEngine.Rendering.RenderPipelineManager.CleanupRenderPipeline () (at <6a3be24f609f4c7e856c987d6a26bd21>:0)

UnityEngine.Rendering.RenderPipelineManager.HandleRenderPipelineChange (UnityEngine.Rendering.RenderPipelineAsset pipelineAsset) (at <6a3be24f609f4c7e856c987d6a26bd21>:0)

UnityEditor.EditorApplication:Internal\_CallDelayFunctions()



Catalog: Catalog TipCatalog knows 12 things

UnityEngine.Logger:Log (UnityEngine.LogType,object)

Shooter.Infrastructure.Logs:WriteToUnityLogger (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:WriteAtLevel (Shooter.Infrastructure.LogLevel,string)

Shooter.Infrastructure.Journal:Info (string)

Shooter.Infrastructure.Catalog`1<Shooter.Game.LoadingScreenTipAsset>:OnEnable () (at Assets/Shooter/Scripts/Infrastructure/Catalogs/Catalog.cs:52)

UnityEditor.EditorApplication:Internal\_CallDelayFunctions ()



Shader warning in 'Shader Graphs/TerrainGrass': pow(f, e) will not work for negative f, use abs(f) or conditionally handle negative values if you expect them at Assets/Shooter/Data/Environment/Props/Nature/Grass/Shaders/TerrainGrass.shadergraph(528) (on dx11)



Compiling Subshader: 0, Pass: ShadowCaster, Vertex program with MATERIAL\_QUALITY\_LOW \_ALPHATEST\_ON \_DOUBLESIDED\_ON

Platform defines: SHADER\_API\_DESKTOP UNITY\_ENABLE\_DETAIL\_NORMALMAP UNITY\_ENABLE\_REFLECTION\_BUFFERS UNITY\_LIGHTMAP\_FULL\_HDR UNITY\_LIGHT\_PROBE\_PROXY\_VOLUME UNITY\_PASS\_SHADOWCASTER UNITY\_PBS\_USE\_BRDF1 UNITY\_PLATFORM\_SUPPORTS\_DEPTH\_FETCH UNITY\_SPECCUBE\_BLENDING UNITY\_SPECCUBE\_BOX\_PROJECTION UNITY\_USE\_DITHER\_MASK\_FOR\_ALPHABLENDED\_SHADOWS

Disabled keywords: DOTS\_INSTANCING\_ON INSTANCING\_ON MATERIAL\_QUALITY\_HIGH MATERIAL\_QUALITY\_MEDIUM SHADER\_API\_GLES30 SHADER\_API\_GLES31 SHADER\_API\_GLES32 UNITY\_ASTC\_NORMALMAP\_ENCODING UNITY\_COLORSPACE\_GAMMA UNITY\_FRAMEBUFFER\_FETCH\_AVAILABLE UNITY\_HARDWARE\_TIER1 UNITY\_HARDWARE\_TIER2 UNITY\_HARDWARE\_TIER3 UNITY\_LIGHTMAP\_DLDR\_ENCODING UNITY\_LIGHTMAP\_RGBM\_ENCODING UNITY\_METAL\_SHADOWS\_USE\_POINT\_FILTERING UNITY\_NO\_DXT5nm UNITY\_NO\_SCREENSPACE\_SHADOWS UNITY\_PBS\_USE\_BRDF2 UNITY\_PBS\_USE\_BRDF3 UNITY\_PRETRANSFORM\_TO\_DISPLAY\_ORIENTATION UNITY\_UNIFIED\_SHADER\_PRECISION\_MODEL UNITY\_VIRTUAL\_TEXTURING \_REFRACTION\_PLANE \_REFRACTION\_SPHERE \_REFRACTION\_THIN



Shader warning in 'Shader Graphs/TerrainGrass': pow(f, e) will not work for negative f, use abs(f) or conditionally handle negative values if you expect them at Assets/Shooter/Data/Environment/Props/Nature/Grass/Shaders/TerrainGrass.shadergraph(528) (on dx12)



Compiling Subshader: 0, Pass: ShadowCaster, Vertex program with INSTANCING\_ON MATERIAL\_QUALITY\_HIGH \_ALPHATEST\_ON \_DOUBLESIDED\_ON

Platform defines: SHADER\_API\_DESKTOP UNITY\_ENABLE\_DETAIL\_NORMALMAP UNITY\_ENABLE\_REFLECTION\_BUFFERS UNITY\_LIGHTMAP\_FULL\_HDR UNITY\_LIGHT\_PROBE\_PROXY\_VOLUME UNITY\_PASS\_SHADOWCASTER UNITY\_PBS\_USE\_BRDF1 UNITY\_PLATFORM\_SUPPORTS\_DEPTH\_FETCH UNITY\_SPECCUBE\_BLENDING UNITY\_SPECCUBE\_BOX\_PROJECTION UNITY\_USE\_DITHER\_MASK\_FOR\_ALPHABLENDED\_SHADOWS

Disabled keywords: DOTS\_INSTANCING\_ON MATERIAL\_QUALITY\_LOW MATERIAL\_QUALITY\_MEDIUM SHADER\_API\_GLES30 SHADER\_API\_GLES31 SHADER\_API\_GLES32 UNITY\_ASTC\_NORMALMAP\_ENCODING UNITY\_COLORSPACE\_GAMMA UNITY\_FRAMEBUFFER\_FETCH\_AVAILABLE UNITY\_HARDWARE\_TIER1 UNITY\_HARDWARE\_TIER2 UNITY\_HARDWARE\_TIER3 UNITY\_LIGHTMAP\_DLDR\_ENCODING UNITY\_LIGHTMAP\_RGBM\_ENCODING UNITY\_METAL\_SHADOWS\_USE\_POINT\_FILTERING UNITY\_NO\_DXT5nm UNITY\_NO\_SCREENSPACE\_SHADOWS UNITY\_PBS\_USE\_BRDF2 UNITY\_PBS\_USE\_BRDF3 UNITY\_PRETRANSFORM\_TO\_DISPLAY\_ORIENTATION UNITY\_UNIFIED\_SHADER\_PRECISION\_MODEL UNITY\_VIRTUAL\_TEXTURING \_REFRACTION\_PLANE \_REFRACTION\_SPHERE \_REFRACTION\_THIN



Shader warning in 'Shader Graphs/TerrainGrass': pow(f, e) will not work for negative f, use abs(f) or conditionally handle negative values if you expect them at Assets/Shooter/Data/Environment/Props/Nature/Grass/Shaders/TerrainGrass.shadergraph(3072) (on dx12)



Compiling Subshader: 0, Pass: ScenePickingPass, Vertex program with INSTANCING\_ON MATERIAL\_QUALITY\_HIGH \_ALPHATEST\_ON \_DOUBLESIDED\_ON

Platform defines: SHADER\_API\_DESKTOP UNITY\_ENABLE\_DETAIL\_NORMALMAP UNITY\_ENABLE\_REFLECTION\_BUFFERS UNITY\_LIGHTMAP\_FULL\_HDR UNITY\_LIGHT\_PROBE\_PROXY\_VOLUME UNITY\_PBS\_USE\_BRDF1 UNITY\_PLATFORM\_SUPPORTS\_DEPTH\_FETCH UNITY\_SPECCUBE\_BLENDING UNITY\_SPECCUBE\_BOX\_PROJECTION UNITY\_USE\_DITHER\_MASK\_FOR\_ALPHABLENDED\_SHADOWS

Disabled keywords: DOTS\_INSTANCING\_ON MATERIAL\_QUALITY\_LOW MATERIAL\_QUALITY\_MEDIUM SHADER\_API\_GLES30 SHADER\_API\_GLES31 SHADER\_API\_GLES32 UNITY\_ASTC\_NORMALMAP\_ENCODING UNITY\_COLORSPACE\_GAMMA UNITY\_FRAMEBUFFER\_FETCH\_AVAILABLE UNITY\_HARDWARE\_TIER1 UNITY\_HARDWARE\_TIER2 UNITY\_HARDWARE\_TIER3 UNITY\_LIGHTMAP\_DLDR\_ENCODING UNITY\_LIGHTMAP\_RGBM\_ENCODING UNITY\_METAL\_SHADOWS\_USE\_POINT\_FILTERING UNITY\_NO\_DXT5nm UNITY\_NO\_SCREENSPACE\_SHADOWS UNITY\_PBS\_USE\_BRDF2 UNITY\_PBS\_USE\_BRDF3 UNITY\_PRETRANSFORM\_TO\_DISPLAY\_ORIENTATION UNITY\_UNIFIED\_SHADER\_PRECISION\_MODEL UNITY\_VIRTUAL\_TEXTURING \_REFRACTION\_PLANE \_REFRACTION\_SPHERE \_REFRACTION\_THIN



Shader warning in 'Shader Graphs/TerrainGrass': pow(f, e) will not work for negative f, use abs(f) or conditionally handle negative values if you expect them at Assets/Shooter/Data/Environment/Props/Nature/Grass/Shaders/TerrainGrass.shadergraph(4630) (on dx12)



Compiling Subshader: 0, Pass: SceneSelectionPass, Vertex program with INSTANCING\_ON MATERIAL\_QUALITY\_HIGH \_ALPHATEST\_ON \_DOUBLESIDED\_ON

Platform defines: SHADER\_API\_DESKTOP UNITY\_ENABLE\_DETAIL\_NORMALMAP UNITY\_ENABLE\_REFLECTION\_BUFFERS UNITY\_LIGHTMAP\_FULL\_HDR UNITY\_LIGHT\_PROBE\_PROXY\_VOLUME UNITY\_PBS\_USE\_BRDF1 UNITY\_PLATFORM\_SUPPORTS\_DEPTH\_FETCH UNITY\_SPECCUBE\_BLENDING UNITY\_SPECCUBE\_BOX\_PROJECTION UNITY\_USE\_DITHER\_MASK\_FOR\_ALPHABLENDED\_SHADOWS

Disabled keywords: DOTS\_INSTANCING\_ON MATERIAL\_QUALITY\_LOW MATERIAL\_QUALITY\_MEDIUM SHADER\_API\_GLES30 SHADER\_API\_GLES31 SHADER\_API\_GLES32 UNITY\_ASTC\_NORMALMAP\_ENCODING UNITY\_COLORSPACE\_GAMMA UNITY\_FRAMEBUFFER\_FETCH\_AVAILABLE UNITY\_HARDWARE\_TIER1 UNITY\_HARDWARE\_TIER2 UNITY\_HARDWARE\_TIER3 UNITY\_LIGHTMAP\_DLDR\_ENCODING UNITY\_LIGHTMAP\_RGBM\_ENCODING UNITY\_METAL\_SHADOWS\_USE\_POINT\_FILTERING UNITY\_NO\_DXT5nm UNITY\_NO\_SCREENSPACE\_SHADOWS UNITY\_PBS\_USE\_BRDF2 UNITY\_PBS\_USE\_BRDF3 UNITY\_PRETRANSFORM\_TO\_DISPLAY\_ORIENTATION UNITY\_UNIFIED\_SHADER\_PRECISION\_MODEL UNITY\_VIRTUAL\_TEXTURING \_REFRACTION\_PLANE \_REFRACTION\_SPHERE \_REFRACTION\_THIN



Shader warning in 'Shader Graphs/TerrainGrass': pow(f, e) will not work for negative f, use abs(f) or conditionally handle negative values if you expect them at Assets/Shooter/Data/Environment/Props/Nature/Grass/Shaders/TerrainGrass.shadergraph(6209) (on dx11)



Compiling Subshader: 0, Pass: MotionVectors, Vertex program with MATERIAL\_QUALITY\_HIGH WRITE\_NORMAL\_BUFFER

Platform defines: SHADER\_API\_DESKTOP UNITY\_ENABLE\_DETAIL\_NORMALMAP UNITY\_ENABLE\_REFLECTION\_BUFFERS UNITY\_LIGHTMAP\_FULL\_HDR UNITY\_LIGHT\_PROBE\_PROXY\_VOLUME UNITY\_PASS\_MOTIONVECTORS UNITY\_PBS\_USE\_BRDF1 UNITY\_PLATFORM\_SUPPORTS\_DEPTH\_FETCH UNITY\_SPECCUBE\_BLENDING UNITY\_SPECCUBE\_BOX\_PROJECTION UNITY\_USE\_DITHER\_MASK\_FOR\_ALPHABLENDED\_SHADOWS

Disabled keywords: DOTS\_INSTANCING\_ON INSTANCING\_ON MATERIAL\_QUALITY\_LOW MATERIAL\_QUALITY\_MEDIUM SHADER\_API\_GLES30 SHADER\_API\_GLES31 SHADER\_API\_GLES32 UNITY\_ASTC\_NORMALMAP\_ENCODING UNITY\_COLORSPACE\_GAMMA UNITY\_FRAMEBUFFER\_FETCH\_AVAILABLE UNITY\_HARDWARE\_TIER1 UNITY\_HARDWARE\_TIER2 UNITY\_HARDWARE\_TIER3 UNITY\_LIGHTMAP\_DLDR\_ENCODING UNITY\_LIGHTMAP\_RGBM\_ENCODING UNITY\_METAL\_SHADOWS\_USE\_POINT\_FILTERING UNITY\_NO\_DXT5nm UNITY\_NO\_SCREENSPACE\_SHADOWS UNITY\_PBS\_USE\_BRDF2 UNITY\_PBS\_USE\_BRDF3 UNITY\_PRETRANSFORM\_TO\_DISPLAY\_ORIENTATION UNITY\_UNIFIED\_SHADER\_PRECISION\_MODEL UNITY\_VIRTUAL\_TEXTURING \_ADD\_PRECOMPUTED\_VELOCITY \_ALPHATEST\_ON \_DOUBLESIDED\_ON \_REFRACTION\_PLANE \_REFRACTION\_SPHERE \_REFRACTION\_THIN \_SURFACE\_TYPE\_TRANSPARENT \_TRANSPARENT\_REFRACTIVE\_SORT \_TRANSPARENT\_WRITES\_MOTION\_VEC



Shader warning in 'Shader Graphs/TerrainGrass': pow(f, e) will not work for negative f, use abs(f) or conditionally handle negative values if you expect them at Assets/Shooter/Data/Environment/Props/Nature/Grass/Shaders/TerrainGrass.shadergraph(10760) (on dx11)



Compiling Subshader: 0, Pass: DepthOnly, Vertex program with INSTANCING\_ON MATERIAL\_QUALITY\_HIGH

Platform defines: SHADER\_API\_DESKTOP UNITY\_ENABLE\_DETAIL\_NORMALMAP UNITY\_ENABLE\_REFLECTION\_BUFFERS UNITY\_LIGHTMAP\_FULL\_HDR UNITY\_LIGHT\_PROBE\_PROXY\_VOLUME UNITY\_PBS\_USE\_BRDF1 UNITY\_PLATFORM\_SUPPORTS\_DEPTH\_FETCH UNITY\_SPECCUBE\_BLENDING UNITY\_SPECCUBE\_BOX\_PROJECTION UNITY\_USE\_DITHER\_MASK\_FOR\_ALPHABLENDED\_SHADOWS

Disabled keywords: DOTS\_INSTANCING\_ON MATERIAL\_QUALITY\_LOW MATERIAL\_QUALITY\_MEDIUM SHADER\_API\_GLES30 SHADER\_API\_GLES31 SHADER\_API\_GLES32 UNITY\_ASTC\_NORMALMAP\_ENCODING UNITY\_COLORSPACE\_GAMMA UNITY\_FRAMEBUFFER\_FETCH\_AVAILABLE UNITY\_HARDWARE\_TIER1 UNITY\_HARDWARE\_TIER2 UNITY\_HARDWARE\_TIER3 UNITY\_LIGHTMAP\_DLDR\_ENCODING UNITY\_LIGHTMAP\_RGBM\_ENCODING UNITY\_METAL\_SHADOWS\_USE\_POINT\_FILTERING UNITY\_NO\_DXT5nm UNITY\_NO\_SCREENSPACE\_SHADOWS UNITY\_PBS\_USE\_BRDF2 UNITY\_PBS\_USE\_BRDF3 UNITY\_PRETRANSFORM\_TO\_DISPLAY\_ORIENTATION UNITY\_UNIFIED\_SHADER\_PRECISION\_MODEL UNITY\_VIRTUAL\_TEXTURING WRITE\_NORMAL\_BUFFER \_ALPHATEST\_ON \_DOUBLESIDED\_ON \_REFRACTION\_PLANE \_REFRACTION\_SPHERE \_REFRACTION\_THIN



Shader warning in 'Shader Graphs/TerrainGrass': pow(f, e) will not work for negative f, use abs(f) or conditionally handle negative values if you expect them at Assets/Shooter/Data/Environment/Props/Nature/Grass/Shaders/TerrainGrass.shadergraph(10760) (on dx12)



Compiling Subshader: 0, Pass: DepthOnly, Vertex program with INSTANCING\_ON MATERIAL\_QUALITY\_HIGH \_ALPHATEST\_ON \_DOUBLESIDED\_ON

Platform defines: SHADER\_API\_DESKTOP UNITY\_ENABLE\_DETAIL\_NORMALMAP UNITY\_ENABLE\_REFLECTION\_BUFFERS UNITY\_LIGHTMAP\_FULL\_HDR UNITY\_LIGHT\_PROBE\_PROXY\_VOLUME UNITY\_PBS\_USE\_BRDF1 UNITY\_PLATFORM\_SUPPORTS\_DEPTH\_FETCH UNITY\_SPECCUBE\_BLENDING UNITY\_SPECCUBE\_BOX\_PROJECTION UNITY\_USE\_DITHER\_MASK\_FOR\_ALPHABLENDED\_SHADOWS

Disabled keywords: DOTS\_INSTANCING\_ON MATERIAL\_QUALITY\_LOW MATERIAL\_QUALITY\_MEDIUM SHADER\_API\_GLES30 SHADER\_API\_GLES31 SHADER\_API\_GLES32 UNITY\_ASTC\_NORMALMAP\_ENCODING UNITY\_COLORSPACE\_GAMMA UNITY\_FRAMEBUFFER\_FETCH\_AVAILABLE UNITY\_HARDWARE\_TIER1 UNITY\_HARDWARE\_TIER2 UNITY\_HARDWARE\_TIER3 UNITY\_LIGHTMAP\_DLDR\_ENCODING UNITY\_LIGHTMAP\_RGBM\_ENCODING UNITY\_METAL\_SHADOWS\_USE\_POINT\_FILTERING UNITY\_NO\_DXT5nm UNITY\_NO\_SCREENSPACE\_SHADOWS UNITY\_PBS\_USE\_BRDF2 UNITY\_PBS\_USE\_BRDF3 UNITY\_PRETRANSFORM\_TO\_DISPLAY\_ORIENTATION UNITY\_UNIFIED\_SHADER\_PRECISION\_MODEL UNITY\_VIRTUAL\_TEXTURING WRITE\_NORMAL\_BUFFER \_REFRACTION\_PLANE \_REFRACTION\_SPHERE \_REFRACTION\_THIN



Shader warning in 'Shader Graphs/TerrainGrass': pow(f, e) will not work for negative f, use abs(f) or conditionally handle negative values if you expect them at Assets/Shooter/Data/Environment/Props/Nature/Grass/Shaders/TerrainGrass.shadergraph(12377) (on dx11)



Compiling Subshader: 0, Pass: GBuffer, Vertex program with MATERIAL\_QUALITY\_HIGH

Platform defines: SHADER\_API\_DESKTOP UNITY\_ENABLE\_DETAIL\_NORMALMAP UNITY\_ENABLE\_REFLECTION\_BUFFERS UNITY\_LIGHTMAP\_FULL\_HDR UNITY\_LIGHT\_PROBE\_PROXY\_VOLUME UNITY\_PBS\_USE\_BRDF1 UNITY\_PLATFORM\_SUPPORTS\_DEPTH\_FETCH UNITY\_SPECCUBE\_BLENDING UNITY\_SPECCUBE\_BOX\_PROJECTION UNITY\_USE\_DITHER\_MASK\_FOR\_ALPHABLENDED\_SHADOWS

Disabled keywords: DEBUG\_DISPLAY DIRLIGHTMAP\_COMBINED DOTS\_INSTANCING\_ON DYNAMICLIGHTMAP\_ON INSTANCING\_ON LIGHTMAP\_BICUBIC\_SAMPLING LIGHTMAP\_ON MATERIAL\_QUALITY\_LOW MATERIAL\_QUALITY\_MEDIUM SHADER\_API\_GLES30 SHADER\_API\_GLES31 SHADER\_API\_GLES32 UNITY\_ASTC\_NORMALMAP\_ENCODING UNITY\_COLORSPACE\_GAMMA UNITY\_FRAMEBUFFER\_FETCH\_AVAILABLE UNITY\_HARDWARE\_TIER1 UNITY\_HARDWARE\_TIER2 UNITY\_HARDWARE\_TIER3 UNITY\_LIGHTMAP\_DLDR\_ENCODING UNITY\_LIGHTMAP\_RGBM\_ENCODING UNITY\_METAL\_SHADOWS\_USE\_POINT\_FILTERING UNITY\_NO\_DXT5nm UNITY\_NO\_SCREENSPACE\_SHADOWS UNITY\_PBS\_USE\_BRDF2 UNITY\_PBS\_USE\_BRDF3 UNITY\_PRETRANSFORM\_TO\_DISPLAY\_ORIENTATION UNITY\_UNIFIED\_SHADER\_PRECISION\_MODEL UNITY\_VIRTUAL\_TEXTURING USE\_LEGACY\_LIGHTMAPS \_ALPHATEST\_ON \_DOUBLESIDED\_ON \_REFRACTION\_PLANE \_REFRACTION\_SPHERE \_REFRACTION\_THIN \_SURFACE\_TYPE\_TRANSPARENT



Shader warning in 'Shader Graphs/TerrainGrass': pow(f, e) will not work for negative f, use abs(f) or conditionally handle negative values if you expect them at Assets/Shooter/Data/Environment/Props/Nature/Grass/Shaders/TerrainGrass.shadergraph(12377) (on dx12)



Compiling Subshader: 0, Pass: GBuffer, Vertex program with DEBUG\_DISPLAY INSTANCING\_ON MATERIAL\_QUALITY\_HIGH \_ALPHATEST\_ON \_DOUBLESIDED\_ON

Platform defines: SHADER\_API\_DESKTOP UNITY\_ENABLE\_DETAIL\_NORMALMAP UNITY\_ENABLE\_REFLECTION\_BUFFERS UNITY\_LIGHTMAP\_FULL\_HDR UNITY\_LIGHT\_PROBE\_PROXY\_VOLUME UNITY\_PBS\_USE\_BRDF1 UNITY\_PLATFORM\_SUPPORTS\_DEPTH\_FETCH UNITY\_SPECCUBE\_BLENDING UNITY\_SPECCUBE\_BOX\_PROJECTION UNITY\_USE\_DITHER\_MASK\_FOR\_ALPHABLENDED\_SHADOWS

Disabled keywords: DIRLIGHTMAP\_COMBINED DOTS\_INSTANCING\_ON DYNAMICLIGHTMAP\_ON LIGHTMAP\_BICUBIC\_SAMPLING LIGHTMAP\_ON MATERIAL\_QUALITY\_LOW MATERIAL\_QUALITY\_MEDIUM SHADER\_API\_GLES30 SHADER\_API\_GLES31 SHADER\_API\_GLES32 UNITY\_ASTC\_NORMALMAP\_ENCODING UNITY\_COLORSPACE\_GAMMA UNITY\_FRAMEBUFFER\_FETCH\_AVAILABLE UNITY\_HARDWARE\_TIER1 UNITY\_HARDWARE\_TIER2 UNITY\_HARDWARE\_TIER3 UNITY\_LIGHTMAP\_DLDR\_ENCODING UNITY\_LIGHTMAP\_RGBM\_ENCODING UNITY\_METAL\_SHADOWS\_USE\_POINT\_FILTERING UNITY\_NO\_DXT5nm UNITY\_NO\_SCREENSPACE\_SHADOWS UNITY\_PBS\_USE\_BRDF2 UNITY\_PBS\_USE\_BRDF3 UNITY\_PRETRANSFORM\_TO\_DISPLAY\_ORIENTATION UNITY\_UNIFIED\_SHADER\_PRECISION\_MODEL UNITY\_VIRTUAL\_TEXTURING USE\_LEGACY\_LIGHTMAPS \_REFRACTION\_PLANE \_REFRACTION\_SPHERE \_REFRACTION\_THIN \_SURFACE\_TYPE\_TRANSPARENT



Shader warning in 'Shader Graphs/TerrainGrass': pow(f, e) will not work for negative f, use abs(f) or conditionally handle negative values if you expect them at Assets/Shooter/Data/Environment/Props/Nature/Grass/Shaders/TerrainGrass.shadergraph(14035) (on dx11)



Compiling Subshader: 0, Pass: Forward, Vertex program with INSTANCING\_ON MATERIAL\_QUALITY\_HIGH \_ALPHATEST\_ON \_DOUBLESIDED\_ON

Platform defines: SHADER\_API\_DESKTOP UNITY\_ENABLE\_DETAIL\_NORMALMAP UNITY\_ENABLE\_REFLECTION\_BUFFERS UNITY\_LIGHTMAP\_FULL\_HDR UNITY\_LIGHT\_PROBE\_PROXY\_VOLUME UNITY\_PBS\_USE\_BRDF1 UNITY\_PLATFORM\_SUPPORTS\_DEPTH\_FETCH UNITY\_SPECCUBE\_BLENDING UNITY\_SPECCUBE\_BOX\_PROJECTION UNITY\_USE\_DITHER\_MASK\_FOR\_ALPHABLENDED\_SHADOWS

Disabled keywords: DEBUG\_DISPLAY DIRLIGHTMAP\_COMBINED DOTS\_INSTANCING\_ON DYNAMICLIGHTMAP\_ON LIGHTMAP\_BICUBIC\_SAMPLING LIGHTMAP\_ON MATERIAL\_QUALITY\_LOW MATERIAL\_QUALITY\_MEDIUM SHADER\_API\_GLES30 SHADER\_API\_GLES31 SHADER\_API\_GLES32 UNITY\_ASTC\_NORMALMAP\_ENCODING UNITY\_COLORSPACE\_GAMMA UNITY\_FRAMEBUFFER\_FETCH\_AVAILABLE UNITY\_HARDWARE\_TIER1 UNITY\_HARDWARE\_TIER2 UNITY\_HARDWARE\_TIER3 UNITY\_LIGHTMAP\_DLDR\_ENCODING UNITY\_LIGHTMAP\_RGBM\_ENCODING UNITY\_METAL\_SHADOWS\_USE\_POINT\_FILTERING UNITY\_NO\_DXT5nm UNITY\_NO\_SCREENSPACE\_SHADOWS UNITY\_PBS\_USE\_BRDF2 UNITY\_PBS\_USE\_BRDF3 UNITY\_PRETRANSFORM\_TO\_DISPLAY\_ORIENTATION UNITY\_UNIFIED\_SHADER\_PRECISION\_MODEL UNITY\_VIRTUAL\_TEXTURING USE\_LEGACY\_LIGHTMAPS \_ADD\_PRECOMPUTED\_VELOCITY \_REFRACTION\_PLANE \_REFRACTION\_SPHERE \_REFRACTION\_THIN \_SURFACE\_TYPE\_TRANSPARENT \_TRANSPARENT\_REFRACTIVE\_SORT \_TRANSPARENT\_WRITES\_MOTION\_VEC



Shader warning in 'Shader Graphs/TerrainGrass': pow(f, e) will not work for negative f, use abs(f) or conditionally handle negative values if you expect them at Assets/Shooter/Data/Environment/Props/Nature/Grass/Shaders/TerrainGrass.shadergraph(14035) (on dx12)



Compiling Subshader: 0, Pass: Forward, Vertex program with DEBUG\_DISPLAY INSTANCING\_ON MATERIAL\_QUALITY\_HIGH \_ALPHATEST\_ON \_DOUBLESIDED\_ON

Platform defines: SHADER\_API\_DESKTOP UNITY\_ENABLE\_DETAIL\_NORMALMAP UNITY\_ENABLE\_REFLECTION\_BUFFERS UNITY\_LIGHTMAP\_FULL\_HDR UNITY\_LIGHT\_PROBE\_PROXY\_VOLUME UNITY\_PBS\_USE\_BRDF1 UNITY\_PLATFORM\_SUPPORTS\_DEPTH\_FETCH UNITY\_SPECCUBE\_BLENDING UNITY\_SPECCUBE\_BOX\_PROJECTION UNITY\_USE\_DITHER\_MASK\_FOR\_ALPHABLENDED\_SHADOWS

Disabled keywords: DIRLIGHTMAP\_COMBINED DOTS\_INSTANCING\_ON DYNAMICLIGHTMAP\_ON LIGHTMAP\_BICUBIC\_SAMPLING LIGHTMAP\_ON MATERIAL\_QUALITY\_LOW MATERIAL\_QUALITY\_MEDIUM SHADER\_API\_GLES30 SHADER\_API\_GLES31 SHADER\_API\_GLES32 UNITY\_ASTC\_NORMALMAP\_ENCODING UNITY\_COLORSPACE\_GAMMA UNITY\_FRAMEBUFFER\_FETCH\_AVAILABLE UNITY\_HARDWARE\_TIER1 UNITY\_HARDWARE\_TIER2 UNITY\_HARDWARE\_TIER3 UNITY\_LIGHTMAP\_DLDR\_ENCODING UNITY\_LIGHTMAP\_RGBM\_ENCODING UNITY\_METAL\_SHADOWS\_USE\_POINT\_FILTERING UNITY\_NO\_DXT5nm UNITY\_NO\_SCREENSPACE\_SHADOWS UNITY\_PBS\_USE\_BRDF2 UNITY\_PBS\_USE\_BRDF3 UNITY\_PRETRANSFORM\_TO\_DISPLAY\_ORIENTATION UNITY\_UNIFIED\_SHADER\_PRECISION\_MODEL UNITY\_VIRTUAL\_TEXTURING USE\_LEGACY\_LIGHTMAPS \_ADD\_PRECOMPUTED\_VELOCITY \_REFRACTION\_PLANE \_REFRACTION\_SPHERE \_REFRACTION\_THIN \_SURFACE\_TYPE\_TRANSPARENT \_TRANSPARENT\_REFRACTIVE\_SORT \_TRANSPARENT\_WRITES\_MOTION\_VEC



Shader warning in 'Shader Graphs/TerrainGrass': pow(f, e) will not work for negative f, use abs(f) or conditionally handle negative values if you expect them at Assets/Shooter/Data/Environment/Props/Nature/Grass/Shaders/TerrainGrass.shadergraph(15609) (on dx11)



Compiling Subshader: 0, Pass: RayTracingPrepass, Vertex program with INSTANCING\_ON MATERIAL\_QUALITY\_HIGH \_ALPHATEST\_ON \_DOUBLESIDED\_ON

Platform defines: SHADER\_API\_DESKTOP UNITY\_ENABLE\_DETAIL\_NORMALMAP UNITY\_ENABLE\_REFLECTION\_BUFFERS UNITY\_LIGHTMAP\_FULL\_HDR UNITY\_LIGHT\_PROBE\_PROXY\_VOLUME UNITY\_PBS\_USE\_BRDF1 UNITY\_PLATFORM\_SUPPORTS\_DEPTH\_FETCH UNITY\_SPECCUBE\_BLENDING UNITY\_SPECCUBE\_BOX\_PROJECTION UNITY\_USE\_DITHER\_MASK\_FOR\_ALPHABLENDED\_SHADOWS

Disabled keywords: MATERIAL\_QUALITY\_LOW MATERIAL\_QUALITY\_MEDIUM SHADER\_API\_GLES30 SHADER\_API\_GLES31 SHADER\_API\_GLES32 UNITY\_ASTC\_NORMALMAP\_ENCODING UNITY\_COLORSPACE\_GAMMA UNITY\_FRAMEBUFFER\_FETCH\_AVAILABLE UNITY\_HARDWARE\_TIER1 UNITY\_HARDWARE\_TIER2 UNITY\_HARDWARE\_TIER3 UNITY\_LIGHTMAP\_DLDR\_ENCODING UNITY\_LIGHTMAP\_RGBM\_ENCODING UNITY\_METAL\_SHADOWS\_USE\_POINT\_FILTERING UNITY\_NO\_DXT5nm UNITY\_NO\_SCREENSPACE\_SHADOWS UNITY\_PBS\_USE\_BRDF2 UNITY\_PBS\_USE\_BRDF3 UNITY\_PRETRANSFORM\_TO\_DISPLAY\_ORIENTATION UNITY\_UNIFIED\_SHADER\_PRECISION\_MODEL UNITY\_VIRTUAL\_TEXTURING \_REFRACTION\_PLANE \_REFRACTION\_SPHERE \_REFRACTION\_THIN



Shader warning in 'HDRP/TerrainLit': use of potentially uninitialized variable (baseColor) at /Users/nikita/Shooter/Library/PackageCache/com.unity.render-pipelines.high-definition@6fc877b69258/Runtime/Material/TerrainLit/TerrainLit\_Splatmap.hlsl(301) (on dx12)



Compiling Subshader: 0, Pass: GBuffer, Fragment program with DEBUG\_DISPLAY DECALS\_3RT INSTANCING\_ON PROBE\_VOLUMES\_L1 RENDERING\_LAYERS \_MASKMAP \_NORMALMAP \_TERRAIN\_8\_LAYERS \_TERRAIN\_BLEND\_HEIGHT \_TERRAIN\_INSTANCED\_PERPIXEL\_NORMAL

Platform defines: SHADER\_API\_DESKTOP UNITY\_ENABLE\_DETAIL\_NORMALMAP UNITY\_ENABLE\_REFLECTION\_BUFFERS UNITY\_LIGHTMAP\_FULL\_HDR UNITY\_LIGHT\_PROBE\_PROXY\_VOLUME UNITY\_PBS\_USE\_BRDF1 UNITY\_PLATFORM\_SUPPORTS\_DEPTH\_FETCH UNITY\_SPECCUBE\_BLENDING UNITY\_SPECCUBE\_BOX\_PROJECTION UNITY\_USE\_DITHER\_MASK\_FOR\_ALPHABLENDED\_SHADOWS

Disabled keywords: DECALS\_4RT DECALS\_OFF DECAL\_SURFACE\_GRADIENT DIRLIGHTMAP\_COMBINED DYNAMICLIGHTMAP\_ON LIGHTMAP\_BICUBIC\_SAMPLING LIGHTMAP\_ON PROBE\_VOLUMES\_L2 SHADER\_API\_GLES30 SHADER\_API\_GLES31 SHADER\_API\_GLES32 SHADOWS\_SHADOWMASK UNITY\_ASTC\_NORMALMAP\_ENCODING UNITY\_COLORSPACE\_GAMMA UNITY\_FRAMEBUFFER\_FETCH\_AVAILABLE UNITY\_HARDWARE\_TIER1 UNITY\_HARDWARE\_TIER2 UNITY\_HARDWARE\_TIER3 UNITY\_LIGHTMAP\_DLDR\_ENCODING UNITY\_LIGHTMAP\_RGBM\_ENCODING UNITY\_METAL\_SHADOWS\_USE\_POINT\_FILTERING UNITY\_NO\_DXT5nm UNITY\_NO\_SCREENSPACE\_SHADOWS UNITY\_PBS\_USE\_BRDF2 UNITY\_PBS\_USE\_BRDF3 UNITY\_PRETRANSFORM\_TO\_DISPLAY\_ORIENTATION UNITY\_UNIFIED\_SHADER\_PRECISION\_MODEL UNITY\_VIRTUAL\_TEXTURING \_ADD\_PRECOMPUTED\_VELOCITY \_ALPHATEST\_ON \_DISABLE\_DECALS \_SPECULAR\_OCCLUSION\_NONE



Shader warning in 'HDRP/Nature/SpeedTree\_Branch': parameter 'multiplyAlpha' is uninitialized when used here \[-Wparameter-usage] at /Users/nikita/Shooter/Library/PackageCache/com.unity.shadergraph@807b608040c3/ShaderGraphLibrary/LODDitheringTransition.hlsl(48) (on dx11)



Compiling Subshader: 1, Pass: VisibilityDXR, RayTracing program with EFFECT\_BILLBOARD \_ALPHATEST\_ON \_DISABLE\_DECALS \_WINDQUALITY\_FASTEST

Platform defines: SHADER\_API\_DESKTOP UNITY\_ENABLE\_DETAIL\_NORMALMAP UNITY\_ENABLE\_REFLECTION\_BUFFERS UNITY\_LIGHTMAP\_FULL\_HDR UNITY\_LIGHT\_PROBE\_PROXY\_VOLUME UNITY\_PBS\_USE\_BRDF1 UNITY\_PLATFORM\_SUPPORTS\_DEPTH\_FETCH UNITY\_SPECCUBE\_BLENDING UNITY\_SPECCUBE\_BOX\_PROJECTION UNITY\_USE\_DITHER\_MASK\_FOR\_ALPHABLENDED\_SHADOWS

Disabled keywords: SHADER\_API\_GLES30 SHADER\_API\_GLES31 SHADER\_API\_GLES32 TRANSPARENT\_COLOR\_SHADOW UNITY\_ASTC\_NORMALMAP\_ENCODING UNITY\_COLORSPACE\_GAMMA UNITY\_FRAMEBUFFER\_FETCH\_AVAILABLE UNITY\_HARDWARE\_TIER1 UNITY\_HARDWARE\_TIER2 UNITY\_HARDWARE\_TIER3 UNITY\_LIGHTMAP\_DLDR\_ENCODING UNITY\_LIGHTMAP\_RGBM\_ENCODING UNITY\_METAL\_SHADOWS\_USE\_POINT\_FILTERING UNITY\_NO\_DXT5nm UNITY\_NO\_SCREENSPACE\_SHADOWS UNITY\_PBS\_USE\_BRDF2 UNITY\_PBS\_USE\_BRDF3 UNITY\_PRETRANSFORM\_TO\_DISPLAY\_ORIENTATION UNITY\_UNIFIED\_SHADER\_PRECISION\_MODEL UNITY\_VIRTUAL\_TEXTURING \_DOUBLESIDED\_ON \_REFRACTION\_PLANE \_REFRACTION\_SPHERE \_REFRACTION\_THIN \_SURFACE\_TYPE\_TRANSPARENT \_WINDQUALITY\_BEST \_WINDQUALITY\_BETTER \_WINDQUALITY\_FAST \_WINDQUALITY\_NONE \_WINDQUALITY\_PALM



Shader warning in 'HDRP/Nature/SpeedTree\_Branch': parameter 'multiplyAlpha' is uninitialized when used here \[-Wparameter-usage] at /Users/nikita/Shooter/Library/PackageCache/com.unity.shadergraph@807b608040c3/ShaderGraphLibrary/LODDitheringTransition.hlsl(48) (on dx12)



Compiling Subshader: 1, Pass: PathTracingDXR, RayTracing program with DECALS\_3RT PROBE\_VOLUMES\_L1 \_ALPHATEST\_ON \_DISABLE\_DECALS \_DISABLE\_SSR \_DISABLE\_SSR\_TRANSPARENT \_DOUBLESIDED\_ON \_MATERIAL\_FEATURE\_TRANSMISSION \_WINDQUALITY\_BEST

Platform defines: SHADER\_API\_DESKTOP UNITY\_ENABLE\_DETAIL\_NORMALMAP UNITY\_ENABLE\_REFLECTION\_BUFFERS UNITY\_LIGHTMAP\_FULL\_HDR UNITY\_LIGHT\_PROBE\_PROXY\_VOLUME UNITY\_PBS\_USE\_BRDF1 UNITY\_PLATFORM\_SUPPORTS\_DEPTH\_FETCH UNITY\_SPECCUBE\_BLENDING UNITY\_SPECCUBE\_BOX\_PROJECTION UNITY\_USE\_DITHER\_MASK\_FOR\_ALPHABLENDED\_SHADOWS

Disabled keywords: DEBUG\_DISPLAY DECALS\_4RT DECALS\_OFF DECAL\_SURFACE\_GRADIENT DIRLIGHTMAP\_COMBINED DYNAMICLIGHTMAP\_ON EFFECT\_BILLBOARD LIGHTMAP\_BICUBIC\_SAMPLING LIGHTMAP\_ON PROBE\_VOLUMES\_L2 SHADER\_API\_GLES30 SHADER\_API\_GLES31 SHADER\_API\_GLES32 UNITY\_ASTC\_NORMALMAP\_ENCODING UNITY\_COLORSPACE\_GAMMA UNITY\_FRAMEBUFFER\_FETCH\_AVAILABLE UNITY\_HARDWARE\_TIER1 UNITY\_HARDWARE\_TIER2 UNITY\_HARDWARE\_TIER3 UNITY\_LIGHTMAP\_DLDR\_ENCODING UNITY\_LIGHTMAP\_RGBM\_ENCODING UNITY\_METAL\_SHADOWS\_USE\_POINT\_FILTERING UNITY\_NO\_DXT5nm UNITY\_NO\_SCREENSPACE\_SHADOWS UNITY\_PBS\_USE\_BRDF2 UNITY\_PBS\_USE\_BRDF3 UNITY\_PRETRANSFORM\_TO\_DISPLAY\_ORIENTATION UNITY\_UNIFIED\_SHADER\_PRECISION\_MODEL UNITY\_VIRTUAL\_TEXTURING \_MATERIAL\_FEATURE\_ANISOTROPY \_MATERIAL\_FEATURE\_COLORED\_TRANSMISSION \_MATERIAL\_FEATURE\_IRIDESCENCE \_MATERIAL\_FEATURE\_SPECULAR\_COLOR \_MATERIAL\_FEATURE\_SUBSURFACE\_SCATTERING \_REFRACTION\_PLANE \_REFRACTION\_SPHERE \_REFRACTION\_THIN \_SURFACE\_TYPE\_TRANSPARENT \_WINDQUALITY\_BETTER \_WINDQUALITY\_FAST \_WINDQUALITY\_FASTEST \_WINDQUALITY\_NONE \_WINDQUALITY\_PALM



Shader warning in 'HDRP/Nature/SpeedTree\_Branch': parameter 'halfAlpha' is uninitialized when used here \[-Wparameter-usage] at /Users/nikita/Shooter/Library/PackageCache/com.unity.shadergraph@807b608040c3/ShaderGraphLibrary/LODDitheringTransition.hlsl(57) (on dx11)



Compiling Subshader: 1, Pass: VisibilityDXR, RayTracing program with EFFECT\_BILLBOARD \_ALPHATEST\_ON \_DISABLE\_DECALS \_WINDQUALITY\_FASTEST

Platform defines: SHADER\_API\_DESKTOP UNITY\_ENABLE\_DETAIL\_NORMALMAP UNITY\_ENABLE\_REFLECTION\_BUFFERS UNITY\_LIGHTMAP\_FULL\_HDR UNITY\_LIGHT\_PROBE\_PROXY\_VOLUME UNITY\_PBS\_USE\_BRDF1 UNITY\_PLATFORM\_SUPPORTS\_DEPTH\_FETCH UNITY\_SPECCUBE\_BLENDING UNITY\_SPECCUBE\_BOX\_PROJECTION UNITY\_USE\_DITHER\_MASK\_FOR\_ALPHABLENDED\_SHADOWS

Disabled keywords: SHADER\_API\_GLES30 SHADER\_API\_GLES31 SHADER\_API\_GLES32 TRANSPARENT\_COLOR\_SHADOW UNITY\_ASTC\_NORMALMAP\_ENCODING UNITY\_COLORSPACE\_GAMMA UNITY\_FRAMEBUFFER\_FETCH\_AVAILABLE UNITY\_HARDWARE\_TIER1 UNITY\_HARDWARE\_TIER2 UNITY\_HARDWARE\_TIER3 UNITY\_LIGHTMAP\_DLDR\_ENCODING UNITY\_LIGHTMAP\_RGBM\_ENCODING UNITY\_METAL\_SHADOWS\_USE\_POINT\_FILTERING UNITY\_NO\_DXT5nm UNITY\_NO\_SCREENSPACE\_SHADOWS UNITY\_PBS\_USE\_BRDF2 UNITY\_PBS\_USE\_BRDF3 UNITY\_PRETRANSFORM\_TO\_DISPLAY\_ORIENTATION UNITY\_UNIFIED\_SHADER\_PRECISION\_MODEL UNITY\_VIRTUAL\_TEXTURING \_DOUBLESIDED\_ON \_REFRACTION\_PLANE \_REFRACTION\_SPHERE \_REFRACTION\_THIN \_SURFACE\_TYPE\_TRANSPARENT \_WINDQUALITY\_BEST \_WINDQUALITY\_BETTER \_WINDQUALITY\_FAST \_WINDQUALITY\_NONE \_WINDQUALITY\_PALM



Shader warning in 'HDRP/Nature/SpeedTree\_Branch': parameter 'halfAlpha' is uninitialized when used here \[-Wparameter-usage] at /Users/nikita/Shooter/Library/PackageCache/com.unity.shadergraph@807b608040c3/ShaderGraphLibrary/LODDitheringTransition.hlsl(57) (on dx12)



Compiling Subshader: 1, Pass: PathTracingDXR, RayTracing program with DECALS\_3RT PROBE\_VOLUMES\_L1 \_ALPHATEST\_ON \_DISABLE\_DECALS \_DISABLE\_SSR \_DISABLE\_SSR\_TRANSPARENT \_DOUBLESIDED\_ON \_MATERIAL\_FEATURE\_TRANSMISSION \_WINDQUALITY\_BEST

Platform defines: SHADER\_API\_DESKTOP UNITY\_ENABLE\_DETAIL\_NORMALMAP UNITY\_ENABLE\_REFLECTION\_BUFFERS UNITY\_LIGHTMAP\_FULL\_HDR UNITY\_LIGHT\_PROBE\_PROXY\_VOLUME UNITY\_PBS\_USE\_BRDF1 UNITY\_PLATFORM\_SUPPORTS\_DEPTH\_FETCH UNITY\_SPECCUBE\_BLENDING UNITY\_SPECCUBE\_BOX\_PROJECTION UNITY\_USE\_DITHER\_MASK\_FOR\_ALPHABLENDED\_SHADOWS

Disabled keywords: DEBUG\_DISPLAY DECALS\_4RT DECALS\_OFF DECAL\_SURFACE\_GRADIENT DIRLIGHTMAP\_COMBINED DYNAMICLIGHTMAP\_ON EFFECT\_BILLBOARD LIGHTMAP\_BICUBIC\_SAMPLING LIGHTMAP\_ON PROBE\_VOLUMES\_L2 SHADER\_API\_GLES30 SHADER\_API\_GLES31 SHADER\_API\_GLES32 UNITY\_ASTC\_NORMALMAP\_ENCODING UNITY\_COLORSPACE\_GAMMA UNITY\_FRAMEBUFFER\_FETCH\_AVAILABLE UNITY\_HARDWARE\_TIER1 UNITY\_HARDWARE\_TIER2 UNITY\_HARDWARE\_TIER3 UNITY\_LIGHTMAP\_DLDR\_ENCODING UNITY\_LIGHTMAP\_RGBM\_ENCODING UNITY\_METAL\_SHADOWS\_USE\_POINT\_FILTERING UNITY\_NO\_DXT5nm UNITY\_NO\_SCREENSPACE\_SHADOWS UNITY\_PBS\_USE\_BRDF2 UNITY\_PBS\_USE\_BRDF3 UNITY\_PRETRANSFORM\_TO\_DISPLAY\_ORIENTATION UNITY\_UNIFIED\_SHADER\_PRECISION\_MODEL UNITY\_VIRTUAL\_TEXTURING \_MATERIAL\_FEATURE\_ANISOTROPY \_MATERIAL\_FEATURE\_COLORED\_TRANSMISSION \_MATERIAL\_FEATURE\_IRIDESCENCE \_MATERIAL\_FEATURE\_SPECULAR\_COLOR \_MATERIAL\_FEATURE\_SUBSURFACE\_SCATTERING \_REFRACTION\_PLANE \_REFRACTION\_SPHERE \_REFRACTION\_THIN \_SURFACE\_TYPE\_TRANSPARENT \_WINDQUALITY\_BETTER \_WINDQUALITY\_FAST \_WINDQUALITY\_FASTEST \_WINDQUALITY\_NONE \_WINDQUALITY\_PALM



Shader warning in 'HDRP/Nature/SpeedTree\_Bark': parameter 'multiplyAlpha' is uninitialized when used here \[-Wparameter-usage] at /Users/nikita/Shooter/Library/PackageCache/com.unity.shadergraph@807b608040c3/ShaderGraphLibrary/LODDitheringTransition.hlsl(48) (on dx11)



Compiling Subshader: 1, Pass: DebugDXR, RayTracing program with <no keywords>

Platform defines: SHADER\_API\_DESKTOP UNITY\_ENABLE\_DETAIL\_NORMALMAP UNITY\_ENABLE\_REFLECTION\_BUFFERS UNITY\_LIGHTMAP\_FULL\_HDR UNITY\_LIGHT\_PROBE\_PROXY\_VOLUME UNITY\_PBS\_USE\_BRDF1 UNITY\_PLATFORM\_SUPPORTS\_DEPTH\_FETCH UNITY\_SPECCUBE\_BLENDING UNITY\_SPECCUBE\_BOX\_PROJECTION UNITY\_USE\_DITHER\_MASK\_FOR\_ALPHABLENDED\_SHADOWS

Disabled keywords: SHADER\_API\_GLES30 SHADER\_API\_GLES31 SHADER\_API\_GLES32 UNITY\_ASTC\_NORMALMAP\_ENCODING UNITY\_COLORSPACE\_GAMMA UNITY\_FRAMEBUFFER\_FETCH\_AVAILABLE UNITY\_HARDWARE\_TIER1 UNITY\_HARDWARE\_TIER2 UNITY\_HARDWARE\_TIER3 UNITY\_LIGHTMAP\_DLDR\_ENCODING UNITY\_LIGHTMAP\_RGBM\_ENCODING UNITY\_METAL\_SHADOWS\_USE\_POINT\_FILTERING UNITY\_NO\_DXT5nm UNITY\_NO\_SCREENSPACE\_SHADOWS UNITY\_PBS\_USE\_BRDF2 UNITY\_PBS\_USE\_BRDF3 UNITY\_PRETRANSFORM\_TO\_DISPLAY\_ORIENTATION UNITY\_UNIFIED\_SHADER\_PRECISION\_MODEL UNITY\_VIRTUAL\_TEXTURING



Shader warning in 'HDRP/Nature/SpeedTree\_Bark': parameter 'multiplyAlpha' is uninitialized when used here \[-Wparameter-usage] at /Users/nikita/Shooter/Library/PackageCache/com.unity.shadergraph@807b608040c3/ShaderGraphLibrary/LODDitheringTransition.hlsl(48) (on dx12)



Compiling Subshader: 1, Pass: VisibilityDXR, RayTracing program with \_DOUBLESIDED\_ON \_WINDQUALITY\_BEST

Platform defines: SHADER\_API\_DESKTOP UNITY\_ENABLE\_DETAIL\_NORMALMAP UNITY\_ENABLE\_REFLECTION\_BUFFERS UNITY\_LIGHTMAP\_FULL\_HDR UNITY\_LIGHT\_PROBE\_PROXY\_VOLUME UNITY\_PBS\_USE\_BRDF1 UNITY\_PLATFORM\_SUPPORTS\_DEPTH\_FETCH UNITY\_SPECCUBE\_BLENDING UNITY\_SPECCUBE\_BOX\_PROJECTION UNITY\_USE\_DITHER\_MASK\_FOR\_ALPHABLENDED\_SHADOWS

Disabled keywords: EFFECT\_BILLBOARD SHADER\_API\_GLES30 SHADER\_API\_GLES31 SHADER\_API\_GLES32 TRANSPARENT\_COLOR\_SHADOW UNITY\_ASTC\_NORMALMAP\_ENCODING UNITY\_COLORSPACE\_GAMMA UNITY\_FRAMEBUFFER\_FETCH\_AVAILABLE UNITY\_HARDWARE\_TIER1 UNITY\_HARDWARE\_TIER2 UNITY\_HARDWARE\_TIER3 UNITY\_LIGHTMAP\_DLDR\_ENCODING UNITY\_LIGHTMAP\_RGBM\_ENCODING UNITY\_METAL\_SHADOWS\_USE\_POINT\_FILTERING UNITY\_NO\_DXT5nm UNITY\_NO\_SCREENSPACE\_SHADOWS UNITY\_PBS\_USE\_BRDF2 UNITY\_PBS\_USE\_BRDF3 UNITY\_PRETRANSFORM\_TO\_DISPLAY\_ORIENTATION UNITY\_UNIFIED\_SHADER\_PRECISION\_MODEL UNITY\_VIRTUAL\_TEXTURING \_DISABLE\_DECALS \_REFRACTION\_PLANE \_REFRACTION\_SPHERE \_REFRACTION\_THIN \_SURFACE\_TYPE\_TRANSPARENT \_WINDQUALITY\_BETTER \_WINDQUALITY\_FAST \_WINDQUALITY\_FASTEST \_WINDQUALITY\_NONE \_WINDQUALITY\_PALM



Shader warning in 'HDRP/Nature/SpeedTree\_Bark': parameter 'halfAlpha' is uninitialized when used here \[-Wparameter-usage] at /Users/nikita/Shooter/Library/PackageCache/com.unity.shadergraph@807b608040c3/ShaderGraphLibrary/LODDitheringTransition.hlsl(57) (on dx11)



Compiling Subshader: 1, Pass: DebugDXR, RayTracing program with <no keywords>

Platform defines: SHADER\_API\_DESKTOP UNITY\_ENABLE\_DETAIL\_NORMALMAP UNITY\_ENABLE\_REFLECTION\_BUFFERS UNITY\_LIGHTMAP\_FULL\_HDR UNITY\_LIGHT\_PROBE\_PROXY\_VOLUME UNITY\_PBS\_USE\_BRDF1 UNITY\_PLATFORM\_SUPPORTS\_DEPTH\_FETCH UNITY\_SPECCUBE\_BLENDING UNITY\_SPECCUBE\_BOX\_PROJECTION UNITY\_USE\_DITHER\_MASK\_FOR\_ALPHABLENDED\_SHADOWS

Disabled keywords: SHADER\_API\_GLES30 SHADER\_API\_GLES31 SHADER\_API\_GLES32 UNITY\_ASTC\_NORMALMAP\_ENCODING UNITY\_COLORSPACE\_GAMMA UNITY\_FRAMEBUFFER\_FETCH\_AVAILABLE UNITY\_HARDWARE\_TIER1 UNITY\_HARDWARE\_TIER2 UNITY\_HARDWARE\_TIER3 UNITY\_LIGHTMAP\_DLDR\_ENCODING UNITY\_LIGHTMAP\_RGBM\_ENCODING UNITY\_METAL\_SHADOWS\_USE\_POINT\_FILTERING UNITY\_NO\_DXT5nm UNITY\_NO\_SCREENSPACE\_SHADOWS UNITY\_PBS\_USE\_BRDF2 UNITY\_PBS\_USE\_BRDF3 UNITY\_PRETRANSFORM\_TO\_DISPLAY\_ORIENTATION UNITY\_UNIFIED\_SHADER\_PRECISION\_MODEL UNITY\_VIRTUAL\_TEXTURING



Shader warning in 'HDRP/Nature/SpeedTree\_Bark': parameter 'halfAlpha' is uninitialized when used here \[-Wparameter-usage] at /Users/nikita/Shooter/Library/PackageCache/com.unity.shadergraph@807b608040c3/ShaderGraphLibrary/LODDitheringTransition.hlsl(57) (on dx12)



Compiling Subshader: 1, Pass: VisibilityDXR, RayTracing program with \_DOUBLESIDED\_ON \_WINDQUALITY\_BEST

Platform defines: SHADER\_API\_DESKTOP UNITY\_ENABLE\_DETAIL\_NORMALMAP UNITY\_ENABLE\_REFLECTION\_BUFFERS UNITY\_LIGHTMAP\_FULL\_HDR UNITY\_LIGHT\_PROBE\_PROXY\_VOLUME UNITY\_PBS\_USE\_BRDF1 UNITY\_PLATFORM\_SUPPORTS\_DEPTH\_FETCH UNITY\_SPECCUBE\_BLENDING UNITY\_SPECCUBE\_BOX\_PROJECTION UNITY\_USE\_DITHER\_MASK\_FOR\_ALPHABLENDED\_SHADOWS

Disabled keywords: EFFECT\_BILLBOARD SHADER\_API\_GLES30 SHADER\_API\_GLES31 SHADER\_API\_GLES32 TRANSPARENT\_COLOR\_SHADOW UNITY\_ASTC\_NORMALMAP\_ENCODING UNITY\_COLORSPACE\_GAMMA UNITY\_FRAMEBUFFER\_FETCH\_AVAILABLE UNITY\_HARDWARE\_TIER1 UNITY\_HARDWARE\_TIER2 UNITY\_HARDWARE\_TIER3 UNITY\_LIGHTMAP\_DLDR\_ENCODING UNITY\_LIGHTMAP\_RGBM\_ENCODING UNITY\_METAL\_SHADOWS\_USE\_POINT\_FILTERING UNITY\_NO\_DXT5nm UNITY\_NO\_SCREENSPACE\_SHADOWS UNITY\_PBS\_USE\_BRDF2 UNITY\_PBS\_USE\_BRDF3 UNITY\_PRETRANSFORM\_TO\_DISPLAY\_ORIENTATION UNITY\_UNIFIED\_SHADER\_PRECISION\_MODEL UNITY\_VIRTUAL\_TEXTURING \_DISABLE\_DECALS \_REFRACTION\_PLANE \_REFRACTION\_SPHERE \_REFRACTION\_THIN \_SURFACE\_TYPE\_TRANSPARENT \_WINDQUALITY\_BETTER \_WINDQUALITY\_FAST \_WINDQUALITY\_FASTEST \_WINDQUALITY\_NONE \_WINDQUALITY\_PALM



Pre-baked collision is disabled on 74 mesh(es) referenced by Mesh Collider components in this build. In future versions of Unity, the build process will no longer automatically pre-bake collision for these meshes; enable pre-baked collision in the Mesh import settings. Use Project Auditor (Window > Analysis > Project Auditor > Game Objects) to find and fix similar issues across the project. Affected meshes:

&#x20;- 'CapConvex\_StalkBolbous\_LOD0\_Type01' (triangle collision is disabled)

&#x20;- 'tv' (triangle collision is disabled)

&#x20;- 'apple' (convex collision is disabled)

&#x20;- 'CapBellShaped\_StalkBolbous\_LOD0\_Type01' (triangle collision is disabled)

&#x20;- 'SM\_Old\_USSR\_Lamp\_01' (triangle collision is disabled)

&#x20;- 'lo\_Prop\_Lamp\_D\_LOD0' (triangle collision is disabled)

&#x20;- 'antique\_light' (triangle collision is disabled)

&#x20;- 'antique\_light.005' (triangle collision is disabled)

&#x20;- 'antique\_light.003' (triangle collision is disabled)

&#x20;- 'antique\_light.006' (triangle collision is disabled)

&#x20;- 'antique\_light.004' (triangle collision is disabled)

&#x20;- 'antique\_light.001' (triangle collision is disabled)

&#x20;- 'antique\_light.002' (triangle collision is disabled)

&#x20;- 'lamp\_large wires' (triangle collision is disabled)

&#x20;- 'lamp\_large body' (triangle collision is disabled)

&#x20;- 'glass lamp\_large' (triangle collision is disabled)

&#x20;- 'lamp\_large lamps' (triangle collision is disabled)

&#x20;- 'lamps' (triangle collision is disabled)

&#x20;- 'glass' (triangle collision is disabled)

&#x20;- 'body' (triangle collision is disabled)

&#x20;- 'table' (triangle collision is disabled)

&#x20;- 'StreetLamp1\_Short' (triangle collision is disabled)

&#x20;- 'stool\_1' (triangle collision is disabled)

&#x20;- 'Geom\_Rock\_Overgrown\_I\_LOD02' (triangle collision is disabled)

&#x20;- 'StreetLamp1\_ShortDouble' (triangle collision is disabled)

&#x20;- 'default' (triangle collision is disabled)

&#x20;- 'Geom\_Rock\_Overgrown\_B\_LOD02' (triangle collision is disabled)

&#x20;- 'stool\_2' (triangle collision is disabled)

&#x20;- 'nightstand\_1' (triangle collision is disabled)

&#x20;- 'door\_knob01' (triangle collision is disabled)

&#x20;- 'UCX\_house\_roof' (triangle collision is disabled)

&#x20;- 'dish\_01\_mesh\_LOD0' (triangle collision is disabled)

&#x20;- 'hourse\_lod2' (triangle collision is disabled)

&#x20;- 'console\_lod2' (triangle collision is disabled)

&#x20;- 'bed' (triangle collision is disabled)

&#x20;- 'default' (triangle collision is disabled)

&#x20;- 'UCX\_house\_aband' (triangle collision is disabled)

&#x20;- 'UCX\_house\_stairs' (triangle collision is disabled)

&#x20;- 'stone\_lod2' (triangle collision is disabled)

&#x20;- 'Geom\_Rock\_Overgrown\_C\_LOD02' (triangle collision is disabled)

&#x20;- 'door\_knob02' (triangle collision is disabled)

&#x20;- 'windowsCeiling' (triangle collision is disabled)

&#x20;- 'polySurface1' (triangle collision is disabled)

&#x20;- 'polySurface2' (triangle collision is disabled)

&#x20;- 'polySurface3' (triangle collision is disabled)

&#x20;- 'polySurface4' (triangle collision is disabled)

&#x20;- 'polySurface5' (triangle collision is disabled)

&#x20;- 'Windows' (triangle collision is disabled)

&#x20;- 'q9:Mesh1' (triangle collision is disabled)

&#x20;- 'q10:Mesh' (triangle collision is disabled)

&#x20;- ...and 24 more (showing the first 50). See Project Auditor for the full list.



Build completed with a result of 'Succeeded' in 61 seconds (61481 ms) \[17.11.2026 22:33:38 -> 17.11.2026 22:34:40, 1m 01s]

UnityEditor.EditorApplication:Internal\_CallDelayFunctions ()



NullReferenceException: Object reference not set to an instance of an object

UnityEngine.Rendering.HighDefinition.HDRenderPipeline.DisposeProbeCameraPool () (at Library/PackageCache/com.unity.render-pipelines.high-definition@6fc877b69258/Runtime/RenderPipeline/HDRenderPipeline.cs:963)

UnityEngine.Rendering.HighDefinition.HDRenderPipeline.Dispose (System.Boolean disposing) (at Library/PackageCache/com.unity.render-pipelines.high-definition@6fc877b69258/Runtime/RenderPipeline/HDRenderPipeline.cs:978)

UnityEngine.Rendering.RenderPipeline.Dispose () (at <6a3be24f609f4c7e856c987d6a26bd21>:0)

UnityEngine.Rendering.RenderPipelineManager.CleanupRenderPipeline () (at <6a3be24f609f4c7e856c987d6a26bd21>:0)

UnityEngine.Rendering.RenderPipelineManager.HandleRenderPipelineChange (UnityEngine.Rendering.RenderPipelineAsset pipelineAsset) (at <6a3be24f609f4c7e856c987d6a26bd21>:0)

UnityEngine.StackTraceUtility:ExtractStringFromExceptionInternal(Object, String\&, String\&)



NullReferenceException: Object reference not set to an instance of an object

UnityEngine.Rendering.HighDefinition.HDRenderPipeline.DisposeProbeCameraPool () (at Library/PackageCache/com.unity.render-pipelines.high-definition@6fc877b69258/Runtime/RenderPipeline/HDRenderPipeline.cs:963)

UnityEngine.Rendering.HighDefinition.HDRenderPipeline.Dispose (System.Boolean disposing) (at Library/PackageCache/com.unity.render-pipelines.high-definition@6fc877b69258/Runtime/RenderPipeline/HDRenderPipeline.cs:978)

UnityEngine.Rendering.RenderPipeline.Dispose () (at <6a3be24f609f4c7e856c987d6a26bd21>:0)

UnityEngine.Rendering.RenderPipelineManager.CleanupRenderPipeline () (at <6a3be24f609f4c7e856c987d6a26bd21>:0)

UnityEngine.Rendering.RenderPipelineManager.HandleRenderPipelineChange (UnityEngine.Rendering.RenderPipelineAsset pipelineAsset) (at <6a3be24f609f4c7e856c987d6a26bd21>:0)

UnityEngine.Rendering.RenderPipelineManager.TryPrepareRenderPipeline (UnityEngine.Rendering.RenderPipelineAsset pipelineAsset) (at <6a3be24f609f4c7e856c987d6a26bd21>:0)

UnityEngine.Rendering.RenderPipelineManager.DoRenderLoop\_Internal (UnityEngine.Rendering.RenderPipelineAsset pipelineAsset, System.IntPtr loopPtr, System.Object renderRequest, Unity.Collections.LowLevel.Unsafe.AtomicSafetyHandle safety) (at <6a3be24f609f4c7e856c987d6a26bd21>:0)

UnityEditor.GUIView:ProcessEvent(EntityId, IntPtr, Boolean\&)



```

