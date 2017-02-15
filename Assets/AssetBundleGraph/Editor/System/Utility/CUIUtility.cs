﻿using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {
	public class CUIUtility {

		private static readonly string kCommandMethod = "AssetBundleGraph.CUIUtility.BuildFromCommandline";

		private static readonly string kCommandStr = 
			"\"{0}\" -batchmode -quit -projectPath \"{1}\" -logFile abbuild.log -executeMethod {2} {3}";

		private static readonly string kCommandName = 
			"buildassetbundle.{0}";

		[MenuItem(Model.Settings.GUI_TEXT_MENU_GENERATE_CUITOOL)]
		private static void CreateCUITool() {

			var appPath = EditorApplication.applicationPath.Replace(Model.Settings.UNITY_FOLDER_SEPARATOR, Path.DirectorySeparatorChar);

            var appCmd = string.Format("{0}{1}", appPath, (Application.platform == RuntimePlatform.WindowsEditor) ? "" : "/Contents/MacOS/Unity");
			var argPass = (Application.platform == RuntimePlatform.WindowsEditor)? "%1 %2 %3 %4 %5 %6 %7 %8 %9" : "$*";
			var cmd = string.Format(kCommandStr, appCmd, FileUtility.ProjectPathWithSlash(), kCommandMethod, argPass);
			var ext = (Application.platform == RuntimePlatform.WindowsEditor)? "bat" : "sh";
			var cmdFile = string.Format(kCommandName, ext );

			var destinationPath = FileUtility.PathCombine(Model.Settings.CUISPACE_PATH, cmdFile);

			Directory.CreateDirectory(Model.Settings.CUISPACE_PATH);
			File.WriteAllText(destinationPath, cmd);

			AssetDatabase.Refresh();
		}

		/**
		 * Build from commandline - entrypoint.
		 */ 
		public static void BuildFromCommandline(){
			try {
				var arguments = new List<string>(System.Environment.GetCommandLineArgs());

				Application.SetStackTraceLogType(LogType.Log, 		StackTraceLogType.None);
				Application.SetStackTraceLogType(LogType.Error, 	StackTraceLogType.None);
				Application.SetStackTraceLogType(LogType.Warning, 	StackTraceLogType.None);

				BuildTarget target = EditorUserBuildSettings.activeBuildTarget;

				int targetIndex = arguments.FindIndex(a => a == "-target");

				if(targetIndex >= 0) {
					var targetStr = arguments[targetIndex+1];
					LogUtility.Logger.Log("Target specified:"+ targetStr);

					var newTarget = BuildTargetUtility.BuildTargetFromString(arguments[targetIndex+1]);
					if(!BuildTargetUtility.IsBuildTargetSupported(newTarget)) {
						throw new AssetBundleGraphException(newTarget + " is not supported to build with this Unity. Please install platform support with installer(s).");
					}

					if(newTarget != target) {
						#if UNITY_5_6
						EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetUtility.TargetToGroup(newTarget), newTarget);
						#else
						EditorUserBuildSettings.SwitchActiveBuildTarget(newTarget);
						#endif
						target = newTarget;
					}
				}

				int graphIndex = arguments.FindIndex(a => a == "-graph");

				Model.ConfigGraph graph = null;

				if(graphIndex >= 0) {
					var graphPath = arguments[targetIndex+1];
					LogUtility.Logger.Log("Graph path:"+ graphPath);

					graph = AssetDatabase.LoadAssetAtPath<Model.ConfigGraph>(graphPath);
				}


				LogUtility.Logger.Log("AssetReference bundle building for:" + BuildTargetUtility.TargetToHumaneString(target));

				if (graph == null) {
					LogUtility.Logger.Log("Graph data not found. To specify graph to execute, use -graph [path]. Aborting...");
					return;
				}

				AssetBundleGraphUtility.ExecuteGraph(target, graph);

			} catch(Exception e) {
				LogUtility.Logger.LogError(LogUtility.kTag, e);
				LogUtility.Logger.LogError(LogUtility.kTag, "Building asset bundles terminated due to unexpected error.");
			} finally {
				LogUtility.Logger.Log("End of build.");
			}
		}
	}
}
