using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Scf.Core.Tests;
using Senparc.Scf.XscfBase.Functions;
using Microsoft.Extensions.DependencyInjection;

namespace Senparc.Scf.XscfBase.Tests
{
    public class FunctionBaseTest_Function : FunctionBase
    {
        public FunctionBaseTest_Function(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public override string Name => "���Է���";

        public override string Description => "���Է���˵��";

        public override Type FunctionParameterType => typeof(FunctionBaseTest_FunctionParameter);

        public override FunctionResult Run(IFunctionParameter param)
        {
            Console.WriteLine("Run");
            FunctionResult result = new FunctionResult()
            {
                Success = true,
                Message = "OK",
                Log = "This is Log"
            };
            return result;
        }
    }

    public class FunctionBaseTest_FunctionParameter : IFunctionParameter
    {
        [Required]
        [MaxLength(300)]
        [System.ComponentModel.Description("·��||��������·�����磺E:\\Senparc\\Scf\\")]
        public string Path { get; set; }

        [MaxLength(100)]
        [System.ComponentModel.Description("�������ռ�||�����ռ����������.��β�������滻[Senparc.Scf.]")]
        public string NewNamespace { get; set; }


        [System.ComponentModel.Description("��վ||ѡ����Ҫ���ص���վ")]
        public SelectionList Site { get; set; } = new SelectionList(SelectionType.DropDownList){
            new SelectionItem("��ѡ��","��ѡ��","ѡ��1"),
            new SelectionItem("GitHub","GitHub","ѡ��2"),
            new SelectionItem("Gitee","Gitee","ѡ��3")
        };
    }

    [TestClass]
    public class FunctionBaseTest : TestBase
    {
        [TestMethod]
        public void GetFunctionParameterInfo()
        {
            FunctionBaseTest_Function function = new FunctionBaseTest_Function(null);
            var paraInfo = function.GetFunctionParameterInfoAsync(base.ServiceCollection.BuildServiceProvider(), false).GetAwaiter().GetResult();

            Assert.AreEqual(3, paraInfo.Count);

            Assert.AreEqual("Path", paraInfo[0].Name);
            Assert.AreEqual("·��", paraInfo[0].Title);
            Assert.AreEqual("��������·�����磺E:\\Senparc\\Scf\\", paraInfo[0].Description);
            Assert.AreEqual(true, paraInfo[0].IsRequired);
            Assert.AreEqual("String", paraInfo[0].SystemType);
            Assert.AreEqual(ParameterType.Text, paraInfo[0].ParameterType);

            Assert.AreEqual("NewNamespace", paraInfo[1].Name);
            Assert.AreEqual("�������ռ�", paraInfo[1].Title);
            Assert.AreEqual(ParameterType.Text, paraInfo[1].ParameterType);
            Assert.AreEqual("�����ռ����������.��β�������滻[Senparc.Scf.]", paraInfo[1].Description);

            Assert.AreEqual(ParameterType.DropDownList, paraInfo[2].ParameterType);
            Assert.AreEqual("Site", paraInfo[2].Name);
            Assert.AreEqual("��վ", paraInfo[2].Title);
            Assert.AreEqual("ѡ����Ҫ���ص���վ", paraInfo[2].Description);
            Assert.AreEqual(3, paraInfo[2].SelectionList.Count());
            Assert.AreEqual("��ѡ��", paraInfo[2].SelectionList[0].Text);
            Assert.AreEqual("GitHub", paraInfo[2].SelectionList[1].Value);
            Assert.AreEqual("Gitee", paraInfo[2].SelectionList[2].Value);

        }
    }
}
