﻿using System.Collections.Generic;
using FubuCore;
using FubuLocalization;
using FubuMVC.Core.Http;
using FubuMVC.Core.Registration;
using FubuMVC.Core.Registration.Querying;
using FubuMVC.Core.UI.Forms;
using FubuMVC.Validation.UI;
using FubuTestingSupport;
using FubuValidation;
using HtmlTags;
using NUnit.Framework;

namespace FubuMVC.Validation.Tests.UI
{
	[TestFixture]
	public class FieldEqualityFormModifierTester
	{
		private BehaviorGraph theGraph;
		private ValidationGraph theValidationGraph;

		private FieldEqualityRule rule1;
		private FieldEqualityRule rule2;

		private StringToken token1;
		private StringToken token2;

		[SetUp]
		public void SetUp()
		{
			token1 = StringToken.FromKeyString("TestKeys:Key1", "Token 1");
			token2 = StringToken.FromKeyString("TestKeys:Key2", "Token 2");

			rule1 = FieldEqualityRule.For<LoFiTarget>(x => x.Value1, x => x.Value2);
			rule1.Token = token1;

			rule2 = FieldEqualityRule.For<LoFiTarget>(x => x.Value1, x => x.Value2);
			rule2.Token = token2;

			var source = new ConfiguredValidationSource(new IValidationRule[] {rule1, rule2});

			theValidationGraph = ValidationGraph.For(source);
			
			theGraph = BehaviorGraph.BuildFrom(x =>
			{
				x.Actions.IncludeType<FormValidationModeEndpoint>();
				x.Import<FubuMvcValidation>();
			});
		}

		private FormRequest requestFor<T>() where T : class, new()
		{
			var services = new InMemoryServiceLocator();
			services.Add<IChainResolver>(new ChainResolutionCache(new TypeResolver(), theGraph));
			services.Add(theValidationGraph);
			services.Add<ICurrentHttpRequest>(new StandInCurrentHttpRequest());

			var request = new FormRequest(new ChainSearch { Type = typeof(T) }, new T());
			request.Attach(services);
			request.ReplaceTag(new FormTag("test"));

			return request;
		}

		[Test]
		public void modifies_the_form()
		{
			var theRequest = requestFor<AjaxTarget>();

			var modifier = new FieldEqualityFormModifier();
			modifier.Modify(theRequest);

			var rawValues = theRequest
				.CurrentTag
				.Data(FieldEqualityFormModifier.FieldEquality)
				.As<IDictionary<string, object>>();

			var values = rawValues.Children("rules");
			values.ShouldHaveCount(2);
		}

		[Test]
		public void no_strategies()
		{
			var theRequest = requestFor<AjaxTarget>();
			theRequest.Chain.ValidationNode().Clear();

			var modifier = new FieldEqualityFormModifier();
			modifier.Modify(theRequest);

			theRequest.CurrentTag.Data(FieldEqualityFormModifier.FieldEquality).ShouldBeNull();
		}
	}
}