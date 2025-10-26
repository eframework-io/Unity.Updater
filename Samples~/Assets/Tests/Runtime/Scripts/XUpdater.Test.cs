// Copyright (c) 2025 EFramework Innovation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using EFramework.Unity.Updater;
using EFramework.Unity.Utility;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// TestXUpdater 是 XUpdater 的单元测试。
/// </summary>
public class TestXUpdater
{
    /// <summary>
    /// 自定义处理器。
    /// </summary>
    public class MyHandler : XUpdater.IHandler
    {
        public MyHandler()
        {
            patchers = new List<XPatcher>
            {
                new MyPatcher(),
                new MyPatcher(),
            };
        }

        public bool EndOnError = false;
        public int RetriedCount = 0;
        public bool CheckResult = true;
        public bool IsInstall = false;

        private XInstaller installer;
        XInstaller XUpdater.IHandler.Installer
        {
            get
            {
                if (installer != null)
                {
                    installer = new XInstaller();
                }
                return installer;
            }
        }

        private List<XPatcher> patchers;
        List<XPatcher> XUpdater.IHandler.Patchers
        {
            get
            {
                patchers ??= new List<XPatcher> { new MyPatcher(), new MyPatcher(), };
                return patchers;
            }
        }

        bool XUpdater.IHandler.OnCheck(out bool install, out bool patch)
        {
            install = IsInstall;
            patch = !IsInstall;
            return CheckResult;
        }

        bool XUpdater.IHandler.OnRetry(XUpdater.Phase phase, XUpdater.IWorker worker, int count, out float wait)
        {
            wait = 0.1f;
            if (EndOnError) return false;
            if (count > 3)
            {
                worker.Error = string.Empty;
                if (phase == XUpdater.Phase.Preprocess) (worker as MyPatcher).ErrorOnPreprocess = false;
                else if (phase == XUpdater.Phase.Process) (worker as MyPatcher).ErrorOnProcess = false;
                else if (phase == XUpdater.Phase.Postprocess) (worker as MyPatcher).ErrorOnPostprocess = false;
                return true;
            }
            RetriedCount++;
            return true;
        }

        public void SetPreprocessError(int index)
        {
            (patchers[index] as MyPatcher).ErrorOnPreprocess = true;
        }

        public void SetProcessError(int index)
        {
            (patchers[index] as MyPatcher).ErrorOnProcess = true;
        }

        public void SetPostprocessError(int index)
        {
            (patchers[index] as MyPatcher).ErrorOnPostprocess = true;
        }
    }

    /// <summary>
    /// 自定义补丁。
    /// </summary>
    public class MyPatcher : XPatcher
    {
        public bool ErrorOnPreprocess;
        public bool ErrorOnProcess;
        public bool ErrorOnPostprocess;

        public MyPatcher() : base("", "", "http://localhost:9000/default/")
        {
            RemoteMani = new XMani.Manifest();
            DiffInfo = new XMani.DiffInfo();
        }

        protected override IEnumerator Extract()
        {
            XUpdater.Event.Notify(XUpdater.EventType.OnPatcherExtractStart);
            XUpdater.Event.Notify(XUpdater.EventType.OnPatcherExtractProgress);
            if (string.IsNullOrEmpty(Error)) XUpdater.Event.Notify(XUpdater.EventType.OnPatcherExtractSucceeded);
            else XUpdater.Event.Notify(XUpdater.EventType.OnPatcherExtractFailed);
            yield return null;
        }

        protected override Func<bool> Validate()
        {
            XUpdater.Event.Notify(XUpdater.EventType.OnPatcherValidateStart);
            XUpdater.Event.Notify(XUpdater.EventType.OnPatcherValidateProgress);
            if (string.IsNullOrEmpty(Error)) XUpdater.Event.Notify(XUpdater.EventType.OnPatcherValidateSucceeded);
            else XUpdater.Event.Notify(XUpdater.EventType.OnPatcherValidateFailed);
            return () => true;
        }

        protected override IEnumerator Download()
        {
            XUpdater.Event.Notify(XUpdater.EventType.OnPatcherDownloadStart);
            XUpdater.Event.Notify(XUpdater.EventType.OnPatcherDownloadProgress);
            if (string.IsNullOrEmpty(Error)) XUpdater.Event.Notify(XUpdater.EventType.OnPatcherDownloadSucceeded);
            else XUpdater.Event.Notify(XUpdater.EventType.OnPatcherDownloadFailed);
            yield return null;
        }

        protected override Func<bool> Cleanup()
        {
            if (string.IsNullOrEmpty(Error)) Debug.Log("MyPatcher.Cleanup Success");
            else
            {
                Error = string.Empty;
                Debug.LogError("MyPatcher.Cleanup Failed");
            }
            return () => true;
        }

        public override IEnumerator Preprocess()
        {
            if (ErrorOnPreprocess) Error = "Preprocess error";
            yield return Extract();
            yield return new WaitUntil(Validate());
        }

        public override IEnumerator Process()
        {
            if (ErrorOnProcess) Error = "Process error";
            yield return Download();
        }

        public override IEnumerator Postprocess()
        {
            if (ErrorOnPostprocess) Error = "Postprocess error";
            yield return Cleanup();
        }
    }

    [UnityTest]
    public IEnumerator Process()
    {
        bool[] isInstalls = new bool[] { true, false };
        foreach (var isInstall in isInstalls)
        {
            var isUpdateStart = false;
            var isUpdateFinish = false;
            var isPatcherStart = false;
            var isPatcherFinish = false;
            var isInstallerStart = false;
            var isInstallerFinish = false;
            var isPatcherExtractStart = false;
            var isPatcherExtractProgress = false;
            var isPatcherExtractSucceeded = false;
            var isPatcherExtractFailed = false;
            var isPatcherValidateStart = false;
            var isPatcherValidateProgress = false;
            var isPatcherValidateSucceeded = false;
            var isPatcherValidateFailed = false;
            var isPatcherDownloadStart = false;
            var isPatcherDownloadUpdate = false;
            var isPatcerhDownloadSucceeded = false;
            var isPatcherDownloadFailed = false;

            var handler = new MyHandler();
            handler.IsInstall = isInstall;
            XUpdater.Event.Register(XUpdater.EventType.OnUpdaterStart, () => isUpdateStart = true);
            XUpdater.Event.Register(XUpdater.EventType.OnPatcherStart, () => isPatcherStart = true);
            XUpdater.Event.Register(XUpdater.EventType.OnPatcherFinish, () => isPatcherFinish = true);
            XUpdater.Event.Register(XUpdater.EventType.OnUpdaterFinish, () => isUpdateFinish = true);
            XUpdater.Event.Register(XUpdater.EventType.OnInstallerStart, () => isInstallerStart = true);
            XUpdater.Event.Register(XUpdater.EventType.OnInstallerFinish, () => isInstallerFinish = true);
            if (!isInstall)
            {
                XUpdater.Event.Register(XUpdater.EventType.OnPatcherExtractStart, () => isPatcherExtractStart = true);
                XUpdater.Event.Register(XUpdater.EventType.OnPatcherExtractProgress, () => isPatcherExtractProgress = true);
                XUpdater.Event.Register(XUpdater.EventType.OnPatcherExtractSucceeded, () => isPatcherExtractSucceeded = true);
                XUpdater.Event.Register(XUpdater.EventType.OnPatcherExtractFailed, () => isPatcherExtractFailed = true);
                XUpdater.Event.Register(XUpdater.EventType.OnPatcherValidateStart, () => isPatcherValidateStart = true);
                XUpdater.Event.Register(XUpdater.EventType.OnPatcherValidateProgress, () => isPatcherValidateProgress = true);
                XUpdater.Event.Register(XUpdater.EventType.OnPatcherValidateSucceeded, () => isPatcherValidateSucceeded = true);
                XUpdater.Event.Register(XUpdater.EventType.OnPatcherValidateFailed, () => isPatcherValidateFailed = true);
                XUpdater.Event.Register(XUpdater.EventType.OnPatcherDownloadStart, () => isPatcherDownloadStart = true);
                XUpdater.Event.Register(XUpdater.EventType.OnPatcherDownloadProgress, () => isPatcherDownloadUpdate = true);
                XUpdater.Event.Register(XUpdater.EventType.OnPatcherDownloadSucceeded, () => isPatcerhDownloadSucceeded = true);
                XUpdater.Event.Register(XUpdater.EventType.OnPatcherDownloadFailed, () => isPatcherDownloadFailed = true);
                LogAssert.Expect(LogType.Log, new Regex("MyPatcher.Cleanup Success"));
            }

            yield return XUpdater.Process(handler);

            Assert.IsTrue(isUpdateStart, "更新开始事件应当被触发。");
            Assert.IsTrue(isUpdateFinish, "更新完成事件应当被触发");
            Assert.AreNotEqual(isInstall, isPatcherStart, "补丁包更新开始事件是否触发应当与 isInstall 相反。");
            Assert.AreNotEqual(isInstall, isPatcherFinish, "补丁包更新完成事件是否触发应当与 isInstall 相反。");
            Assert.AreEqual(isInstall, isInstallerStart, "安装包更新开始是否触发应当与 isInstall 一致。");
            Assert.AreEqual(isInstall, isInstallerFinish, "安装包更新完成是否触发应当与 isInstall 一致。");
            if (!isInstall)
            {
                Assert.IsTrue(isPatcherExtractStart, "补丁包提取开始事件应当被触发。");
                Assert.IsTrue(isPatcherExtractProgress, "补丁包提取更新事件应当被触发。");
                Assert.IsTrue(isPatcherExtractSucceeded, "补丁包提取成功事件应当被触发。");
                Assert.IsFalse(isPatcherExtractFailed, "补丁包提取失败事件应当不被触发。");
                Assert.IsTrue(isPatcherValidateStart, "补丁包校验开始事件应当被触发。");
                Assert.IsTrue(isPatcherValidateProgress, "补丁包校验更新事件应当被触发。");
                Assert.IsTrue(isPatcherValidateSucceeded, "补丁包校验成功事件应当被触发。");
                Assert.IsFalse(isPatcherValidateFailed, "补丁包校验失败事件应当不被触发。");
                Assert.IsTrue(isPatcherDownloadStart, "补丁包下载开始事件应当被触发。");
                Assert.IsTrue(isPatcherDownloadUpdate, "补丁包下载更新事件应当被触发。");
                Assert.IsTrue(isPatcerhDownloadSucceeded, "补丁包下载成功事件应当被触发。");
                Assert.IsFalse(isPatcherDownloadFailed, "补丁包下载失败事件应当不被触发。");
            }
        }
    }

    [UnityTest]
    public IEnumerator OnCheck()
    {
        bool[] results = new bool[] { true, false };
        foreach (var result in results)
        {
            var isUpdateStart = false;
            var isUpdateFinish = false;
            XUpdater.Event.Register(XUpdater.EventType.OnUpdaterStart, () => isUpdateStart = true);
            XUpdater.Event.Register(XUpdater.EventType.OnUpdaterFinish, () => isUpdateFinish = true);
            var handler = new MyHandler();
            handler.CheckResult = result;
            yield return XUpdater.Process(handler);
            Assert.AreEqual(true, isUpdateStart, "更新开始事件应当始终被触发。");
            Assert.AreEqual(result, isUpdateFinish, "更新完成事件是否触发应当与 checkResult 一致。");
        }
    }

    [UnityTest]
    public IEnumerator OnRetry()
    {
        var handler = new MyHandler();

        handler.SetPreprocessError(0);
        yield return XUpdater.Process(handler);
        Assert.AreEqual(3, handler.RetriedCount, "重试次数应当为 3。");

        handler.RetriedCount = 0;
        handler.SetPreprocessError(0);
        handler.SetProcessError(0);
        yield return XUpdater.Process(handler);
        Assert.AreEqual(6, handler.RetriedCount, "重试次数应当为 6。");

        handler.RetriedCount = 0;
        handler.EndOnError = true;
        handler.SetPreprocessError(0);
        yield return XUpdater.Process(handler);
        Assert.AreEqual(0, handler.RetriedCount, "重试次数应当为 0。");
    }

    [UnityTest]
    public IEnumerator OnEvent()
    {
        var handler = new MyHandler();

        var isExtractFailed = false;
        var isValidateFailed = false;
        var isDownloadFailed = false;
        XUpdater.Event.Register(XUpdater.EventType.OnPatcherExtractFailed, () => isExtractFailed = true);
        XUpdater.Event.Register(XUpdater.EventType.OnPatcherValidateFailed, () => isValidateFailed = true);
        XUpdater.Event.Register(XUpdater.EventType.OnPatcherDownloadFailed, () => isDownloadFailed = true);
        handler.SetPreprocessError(0);
        handler.SetProcessError(0);
        LogAssert.Expect(LogType.Error, new Regex("MyPatcher.Cleanup Failed"));
        handler.SetPostprocessError(0);
        yield return XUpdater.Process(handler);
        Assert.IsTrue(isExtractFailed, "补丁提取失败事件应当被触发。");
        Assert.IsTrue(isValidateFailed, "补丁校验失败事件应当被触发。");
        Assert.IsTrue(isDownloadFailed, "补丁下载失败事件应当被触发。");
    }
}
