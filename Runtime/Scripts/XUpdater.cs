// Copyright (c) 2025 EFramework Innovation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EFramework.Unity.Utility;

namespace EFramework.Unity.Updater
{
    /// <summary>
    /// XUpdater 提供了更新流程控制和事件通知，实现了可扩展的业务处理接口。
    /// </summary>
    /// <remarks>
    /// <code>
    /// 功能特性
    /// - 流程控制：支持业务处理接口，提供版本检查和重试策略
    /// - 事件通知：覆盖更新全流程的事件回调，支持进度监控
    /// 
    /// 使用手册
    /// 1. 实现业务处理
    /// 
    /// public class MyHandler : XUpdater.IHandler
    /// {
    ///     private XInstaller installer;
    ///     private List&lt;XPatcher&gt; patchers = new List&lt;XPatcher&gt;();
    ///     
    ///     // Installer 获取安装包更新处理器。
    ///     public XInstaller Installer =&gt; installer;
    ///     
    ///     // Patchers 获取补丁包更新处理器列表。
    ///     public List&lt;XPatcher&gt; Patchers =&gt; patchers;
    ///     
    ///     // OnCheck 检查更新的状态。
    ///     public bool OnCheck(out bool install, out bool patch)
    ///     {
    ///         // 实现检查逻辑，例如：
    ///         install = false;  // 是否进行安装包更新
    ///         patch = true;    // 是否进行补丁包更新
    ///         
    ///         // 如果需要更新补丁包，初始化补丁包处理器
    ///         if (patch)
    ///         {
    ///             var worker = new XPatcher(
    ///                 "StreamingAssets/Patch.zip",  // 内置地址
    ///                 "Local/Patch/Manifest.db",              // 本地地址
    ///                 "https://example.com/Patch/Manifest.db"  // 远端地址
    ///             );
    ///             patches.Add(worker);
    ///         }
    ///         
    ///         return install || patch;  // 返回是否需要更新
    ///     }
    ///     
    ///     // 处理重试逻辑
    ///     public bool OnRetry(XUpdater.Phase phase, XUpdater.IWorker worker, int count, out float pending)
    ///     {
    ///         // 实现重试逻辑，例如：
    ///         pending = 1.0f;  // 重试等待时间（秒）
    ///         return count &lt; 3;  // 最多重试 3 次
    ///     }
    /// }
    /// 
    /// 2. 执行更新流程
    /// 
    /// // 创建流程处理器
    /// var handler = new MyHandler();
    /// 
    /// // 执行更新流程
    /// yield return XUpdater.Process(handler);
    /// 
    /// // 更新完成后的处理
    /// if (string.IsNullOrEmpty(handler.Patches[0].Error))
    /// {
    ///     Debug.Log("更新成功，可以继续游戏流程");
    /// }
    /// else
    /// {
    ///     Debug.LogError($"更新失败：{handler.Patches[0].Error}");
    /// }
    /// 
    /// 3. 监听更新事件
    /// 
    /// // 注册事件监听器
    /// XUpdater.Event.Register(XUpdater.EventType.OnUpdaterStart, OnUpdaterStart);
    /// XUpdater.Event.Register(XUpdater.EventType.OnPatcherDownloadProgress, OnPatcherDownloadProgress);
    /// XUpdater.Event.Register(XUpdater.EventType.OnUpdaterFinish, OnUpdaterFinish);
    /// 
    /// // 更新开始事件处理
    /// private void OnUpdaterStart(object sender)
    /// {
    ///     Debug.Log("更新开始");
    /// }
    /// 
    /// // 补丁下载进度事件处理
    /// private void OnPatcherDownloadProgress(object sender)
    /// {
    ///     var patcher = sender as XPatcher;
    ///     var progress = patcher.Progress(XPatcher.Phase.Download);
    ///     var speed = patcher.Speed(XPatcher.Phase.Download);
    ///     Debug.Log($"下载进度：{progress:P2}，速度：{speed / 1024} KB/s");
    /// }
    /// 
    /// // 更新完成事件处理
    /// private void OnUpdaterFinish(object sender)
    /// {
    ///     Debug.Log("更新完成");
    /// }
    /// </code>
    /// 更多信息请参考模块文档。
    /// </remarks>
    public partial class XUpdater
    {
        /// <summary>
        /// Phase 是更新处理阶段的枚举类型。
        /// </summary>
        public enum Phase
        {
            /// <summary>
            /// Preprocess 是预处理阶段，进行更新前的准备工作。
            /// </summary>
            Preprocess,

            /// <summary>
            /// Process 是处理阶段，执行实际的更新操作。
            /// </summary>
            Process,

            /// <summary>
            /// Postprocess 是后处理阶段，完成更新后的清理工作。
            /// </summary>
            Postprocess,
        }

        /// <summary>
        /// IWorker 是更新流程的业务主体，分阶段实现了具体的更新流程。
        /// </summary>
        public interface IWorker
        {
            /// <summary>
            /// Error 表示错误的信息。
            /// </summary>
            string Error { get; set; }

            /// <summary>
            /// Preprocess 是流程的预处理函数。
            /// </summary>
            /// <returns></returns>
            IEnumerator Preprocess();

            /// <summary>
            /// Process 是流程的处理函数。
            /// </summary>
            /// <returns></returns>
            IEnumerator Process();

            /// <summary>
            /// Postprocess 是流程的后处理函数。
            /// </summary>
            /// <returns></returns>
            IEnumerator Postprocess();
        }

        /// <summary>
        /// IHandler 是更新处理程序接口，定义了更新过程中需要实现的功能。
        /// </summary>
        public interface IHandler
        {
            /// <summary>
            /// Installer 获取安装包更新处理器。
            /// </summary>
            XInstaller Installer { get; }

            /// <summary>
            /// Patchers 获取补丁包更新处理器列表。
            /// </summary>
            List<XPatcher> Patchers { get; }

            /// <summary>
            /// OnCheck 检查更新的状态。
            /// </summary>
            /// <param name="install">输出是否进行安装包更新</param>
            /// <param name="patch">输出是否进行补丁包更新</param>
            /// <returns>返回是否需要更新</returns>
            bool OnCheck(out bool install, out bool patch);

            /// <summary>
            /// OnRetry 处理更新失败时的重试逻辑。
            /// </summary>
            /// <param name="phase">当前处理阶段</param>
            /// <param name="worker">当前更新处理器</param>
            /// <param name="count">已重试次数</param>
            /// <param name="pending">输出重试等待时间（秒）</param>
            /// <returns>返回是否继续重试</returns>
            bool OnRetry(Phase phase, IWorker worker, int count, out float pending);
        }

        /// <summary>
        /// EventType 是内置的事件类型枚举，定义了更新过程中的所有事件。
        /// </summary>
        public enum EventType
        {
            /// <summary>
            /// OnUpdaterStart 表示更新流程开始。
            /// </summary>
            OnUpdaterStart,

            /// <summary>
            /// OnInstallerStart 表示安装包更新流程开始。
            /// </summary>
            OnInstallerStart,

            /// <summary>
            /// OnInstallerDownloadStart 表示安装包文件下载开始。
            /// </summary>
            OnInstallerDownloadStart,

            /// <summary>
            /// OnInstallerDownloadProgress 表示安装包文件下载进度更新。
            /// </summary>
            OnInstallerDownloadProgress,

            /// <summary>
            /// OnInstallerDownloadSucceeded 表示安装包文件下载成功。
            /// </summary>
            OnInstallerDownloadSucceeded,

            /// <summary>
            /// OnInstallerDownloadFailed 表示安装包文件下载失败。
            /// </summary>
            OnInstallerDownloadFailed,

            /// <summary>
            /// OnInstallerExtractStart 表示安装包文件提取开始。
            /// </summary>
            OnInstallerExtractStart,

            /// <summary>
            /// OnInstallerExtractProgress 表示安装包文件提取进度更新。
            /// </summary>
            OnInstallerExtractProgress,

            /// <summary>
            /// OnInstallerExtractSucceeded 表示安装包文件提取成功。
            /// </summary>
            OnInstallerExtractSucceeded,

            /// <summary>
            /// OnInstallerExtractFailed 表示安装包文件提取失败。
            /// </summary>
            OnInstallerExtractFailed,

            /// <summary>
            /// OnInstallerInstallStart 表示安装包文件安装开始。
            /// </summary>
            OnInstallerInstallStart,

            /// <summary>
            /// OnInstallerInstallProgress 表示安装包文件安装进度更新。
            /// </summary>
            OnInstallerInstallProgress,

            /// <summary>
            /// OnInstallerInstallSucceeded 表示安装包文件安装成功。
            /// </summary>
            OnInstallerInstallSucceeded,

            /// <summary>
            /// OnInstallerInstallFailed 表示安装包文件安装失败。
            /// </summary>
            OnInstallerInstallFailed,

            /// <summary>
            /// OnInstallerFinish 表示安装包更新流程完成。
            /// </summary>
            OnInstallerFinish,

            /// <summary>
            /// OnPatcherStart 表示补丁包更新流程开始。
            /// </summary>
            OnPatcherStart,

            /// <summary>
            /// OnPatcherExtractStart 表示补丁包文件提取开始。
            /// </summary>
            OnPatcherExtractStart,

            /// <summary>
            /// OnPatcherExtractProgress 表示补丁包文件提取进度更新。
            /// </summary>
            OnPatcherExtractProgress,

            /// <summary>
            /// OnPatcherExtractSucceeded 表示补丁包文件提取成功。
            /// </summary>
            OnPatcherExtractSucceeded,

            /// <summary>
            /// OnPatcherExtractFailed 表示补丁包文件提取失败。
            /// </summary>
            OnPatcherExtractFailed,

            /// <summary>
            /// OnPatcherValidateStart 表示补丁包文件验证开始。
            /// </summary>
            OnPatcherValidateStart,

            /// <summary>
            /// OnPatcherValidateProgress 表示补丁包文件验证进度更新。
            /// </summary>
            OnPatcherValidateProgress,

            /// <summary>
            /// OnPatcherValidateSucceeded 表示补丁包文件验证成功。
            /// </summary>
            OnPatcherValidateSucceeded,

            /// <summary>
            /// OnPatcherValidateFailed 表示补丁包文件验证失败。
            /// </summary>
            OnPatcherValidateFailed,

            /// <summary>
            /// OnPatcherDownloadStart 表示补丁包文件下载开始。
            /// </summary>
            OnPatcherDownloadStart,

            /// <summary>
            /// OnPatcherDownloadProgress 表示补丁包文件下载进度更新。
            /// </summary>
            OnPatcherDownloadProgress,

            /// <summary>
            /// OnPatcherDownloadSucceeded 表示补丁包文件下载成功。
            /// </summary>
            OnPatcherDownloadSucceeded,

            /// <summary>
            /// OnPatcherDownloadFailed 表示补丁包文件下载失败。
            /// </summary>
            OnPatcherDownloadFailed,

            /// <summary>
            /// OnPatcherFinish 表示补丁包更新流程完成。
            /// </summary>
            OnPatcherFinish,

            /// <summary>
            /// OnUpdaterFinish 表示更新流程完成。
            /// </summary>
            OnUpdaterFinish,
        }

        /// <summary>
        /// Event 是事件管理器实例，用于处理更新过程中的事件通知。
        /// </summary>
        public static readonly XEvent.Manager Event = new();

        /// <summary>
        /// Process 处理更新的主方法，协调整个更新流程的执行。
        /// </summary>
        /// <param name="handler">更新处理程序实例</param>
        /// <returns>返回一个协程</returns>
        /// <exception cref="ArgumentNullException">处理程序为空时抛出</exception>
        public static IEnumerator Process(IHandler handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");
            Event.Notify(EventType.OnUpdaterStart);

            if (handler.OnCheck(out var install, out var patch))
            {
                XLog.Notice("XUpdater.Process: start to process installer: {0}, patcher: {1}.", install, patch);
                if (install)
                {
                    Event.Notify(EventType.OnInstallerStart);

                    if (handler.Installer == null)
                    {
                        XLog.Warn("XUpdater.Process: no installer worker was found for updating.");
                    }
                    else
                    {
                        #region Preprocess
                        {
                            var succeeded = false;
                            var executeCount = 0;
                            while (!succeeded)
                            {
                                yield return handler.Installer.Preprocess();
                                executeCount++;
                                if (!string.IsNullOrEmpty(handler.Installer.Error))
                                {
                                    if (handler.OnRetry(Phase.Preprocess, handler.Installer, executeCount, out var pending)) yield return new WaitForSeconds(pending);
                                    else yield break;
                                }
                                else succeeded = true;
                            }
                        }
                        #endregion

                        #region Process
                        {
                            var succeeded = false;
                            var executeCount = 0;
                            while (!succeeded)
                            {
                                yield return handler.Installer.Process();
                                executeCount++;
                                if (!string.IsNullOrEmpty(handler.Installer.Error))
                                {
                                    if (handler.OnRetry(Phase.Process, handler.Installer, executeCount, out var pending)) yield return new WaitForSeconds(pending);
                                    else yield break;
                                }
                                else succeeded = true;
                            }
                        }
                        #endregion

                        #region Postprocess
                        {
                            var succeeded = false;
                            var executeCount = 0;
                            while (!succeeded)
                            {
                                yield return handler.Installer.Postprocess();
                                executeCount++;
                                if (!string.IsNullOrEmpty(handler.Installer.Error))
                                {
                                    if (handler.OnRetry(Phase.Postprocess, handler.Installer, executeCount, out var pending)) yield return new WaitForSeconds(pending);
                                    else yield break;
                                }
                                else succeeded = true;
                            }
                        }
                        #endregion
                    }

                    Event.Notify(EventType.OnInstallerFinish);
                    XLog.Notice("XUpdater.Process: process installer finish.");
                }
                else if (patch)
                {
                    Event.Notify(EventType.OnPatcherStart);
                    if (handler.Patchers == null || handler.Patchers.Count == 0)
                    {
                        Event.Notify(EventType.OnPatcherFinish);
                        Event.Notify(EventType.OnUpdaterFinish);
                        XLog.Warn("XUpdater.Process: no patcher was found for updating.");
                    }
                    else
                    {
                        #region Preprocess
                        {
                            var executeCount = 0;
                            XPatcher lworker = null;
                            for (var i = 0; i < handler.Patchers.Count;)
                            {
                                var worker = handler.Patchers[i];
                                if (lworker != worker)
                                {
                                    lworker = worker;
                                    executeCount = 1;
                                }
                                else executeCount++;
                                yield return worker.Preprocess();
                                if (!string.IsNullOrEmpty(worker.Error))
                                {
                                    if (handler.OnRetry(Phase.Preprocess, worker, executeCount, out var pending)) yield return new WaitForSeconds(pending);
                                    else yield break;
                                }
                                else i++;
                            }
                        }
                        #endregion

                        #region Process
                        {
                            var executeCount = 0;
                            XPatcher lworker = null;
                            for (var i = 0; i < handler.Patchers.Count;)
                            {
                                var worker = handler.Patchers[i];
                                if (lworker != worker)
                                {
                                    lworker = worker;
                                    executeCount = 1;
                                }
                                else executeCount++;
                                yield return worker.Process();
                                if (!string.IsNullOrEmpty(worker.Error))
                                {
                                    if (handler.OnRetry(Phase.Process, worker, executeCount, out var pending)) yield return new WaitForSeconds(pending);
                                    else yield break;
                                }
                                else i++;
                            }
                        }
                        #endregion

                        #region Postprocess
                        {
                            var executeCount = 0;
                            XPatcher lworker = null;
                            for (var i = 0; i < handler.Patchers.Count;)
                            {
                                var worker = handler.Patchers[i];
                                if (lworker != worker)
                                {
                                    lworker = worker;
                                    executeCount = 1;
                                }
                                else executeCount++;
                                yield return worker.Postprocess();
                                if (!string.IsNullOrEmpty(worker.Error))
                                {
                                    if (handler.OnRetry(Phase.Postprocess, worker, executeCount, out var pending)) yield return new WaitForSeconds(pending);
                                    else yield break;
                                }
                                else i++;
                            }
                        }
                        #endregion

                        var manis = new Dictionary<XMani.Manifest, XMani.DiffInfo>();
                        foreach (var tpatch in handler.Patchers) manis.Add(tpatch.RemoteMani, tpatch.DiffInfo);
                        Event.Notify(EventType.OnPatcherFinish, manis);
                        XLog.Notice("XUpdater.Process: process patcher finish.");
                    }
                }

                Event.Notify(EventType.OnUpdaterFinish);
                XLog.Notice("XUpdater.Process: finish to process installer: {0}, patcher: {1}.", install, patch);
            }
        }
    }
}
