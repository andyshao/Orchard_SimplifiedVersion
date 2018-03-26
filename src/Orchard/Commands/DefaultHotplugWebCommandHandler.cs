using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotplugWeb.Localization;
using HotplugWeb.Exceptions;

namespace HotplugWeb.Commands {
    public abstract class DefaultHotplugWebCommandHandler : ICommandHandler {
        protected DefaultHotplugWebCommandHandler() {
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }
        public CommandContext Context { get; set; }

        public void Execute(CommandContext context) {
            SetSwitchValues(context);
            Invoke(context);
        }

        private void SetSwitchValues(CommandContext context) {
            if (context.Switches != null && context.Switches.Any()) {
                foreach (var commandSwitch in context.Switches) {
                    SetSwitchValue(commandSwitch);
                }
            }
        }

        private void SetSwitchValue(KeyValuePair<string, string> commandSwitch) {
            PropertyInfo propertyInfo = GetType().GetProperty(commandSwitch.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (propertyInfo == null) {
                throw new InvalidOperationException(T("没有找到开关\"{0}\"", commandSwitch.Key).Text);
            }
            if (propertyInfo.GetCustomAttributes(typeof(HotplugWebSwitchAttribute), false).Length == 0) {
                throw new InvalidOperationException(T("属性\"{0}\"存在,但未用\"{1}\"进行修饰。", commandSwitch.Key, typeof(HotplugWebSwitchAttribute).Name).Text);
            }

            // 设置值
            try
            {
                object value = Convert.ChangeType(commandSwitch.Value, propertyInfo.PropertyType);
                propertyInfo.SetValue(this, value, null/*index*/);
            }
            catch(Exception ex) {
                if (ex.IsFatal()) {
                    throw;
                } 
                string message = T("转换值\"{0}\"到\"{1}\"的操作错误, 用于切换\"{2}\"",
                    LocalizedString.TextOrDefault(commandSwitch.Value, T("(empty)")), 
                    propertyInfo.PropertyType.FullName, 
                    commandSwitch.Key).Text;
                throw new InvalidOperationException(message, ex);
            }
        }

        private void Invoke(CommandContext context) {
            CheckMethodForSwitches(context.CommandDescriptor.MethodInfo, context.Switches);

            var arguments = (context.Arguments ?? Enumerable.Empty<string>()).ToArray();
            object[] invokeParameters = GetInvokeParametersForMethod(context.CommandDescriptor.MethodInfo, arguments);
            if (invokeParameters == null) {
                throw new InvalidOperationException(T("命令参数\"{0}\"不匹配命令定义", string.Join(" ", arguments)).ToString());
            }

            this.Context = context;
            var result = context.CommandDescriptor.MethodInfo.Invoke(this, invokeParameters);
            if (result is string)
                context.Output.Write(result);
        }

        private static object[] GetInvokeParametersForMethod(MethodInfo methodInfo, IList<string> arguments) {
            var invokeParameters = new List<object>();
            var args = new List<string>(arguments);
            var methodParameters = methodInfo.GetParameters();
            bool methodHasParams = false;

            if (methodParameters.Length == 0) {
                if (args.Count == 0)
                    return invokeParameters.ToArray();
                return null;
            }

            if (methodParameters[methodParameters.Length - 1].ParameterType.IsAssignableFrom(typeof(string[]))) {
                methodHasParams = true;
            }

            if (!methodHasParams && args.Count != methodParameters.Length) return null;
            if (methodHasParams && (methodParameters.Length - args.Count >= 2)) return null;

            for (int i = 0; i < args.Count; i++) {
                if (methodParameters[i].ParameterType.IsAssignableFrom(typeof(string[]))) {
                    invokeParameters.Add(args.GetRange(i, args.Count - i).ToArray());
                    break;
                }
                invokeParameters.Add(Convert.ChangeType(arguments[i], methodParameters[i].ParameterType));
            }

            if (methodHasParams && (methodParameters.Length - args.Count == 1)) invokeParameters.Add(new string[] { });

            return invokeParameters.ToArray();
        }

        private void CheckMethodForSwitches(MethodInfo methodInfo, IDictionary<string, string> switches) {
            if (switches == null || switches.Count == 0)
                return;

            var supportedSwitches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (HotplugWebSwitchesAttribute switchesAttribute in methodInfo.GetCustomAttributes(typeof(HotplugWebSwitchesAttribute), false)) {
                supportedSwitches.UnionWith(switchesAttribute.Switches);
            }

            foreach (var commandSwitch in switches.Keys) {
                if (!supportedSwitches.Contains(commandSwitch)) {
                    throw new InvalidOperationException(T("方法\"{0}\"不支持开关\"{1}\".", methodInfo.Name, commandSwitch).ToString());
                }
            }
        }
    }
}
