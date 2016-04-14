﻿using System;
using System.Linq;
using System.Web.Mvc;
using FeatherWidgets.TestUtilities.CommonOperations;
using MbUnit.Framework;
using Telerik.Sitefinity.DynamicModules;
using Telerik.Sitefinity.DynamicModules.Model;
using Telerik.Sitefinity.Frontend.Media.Mvc.Controllers;
using Telerik.Sitefinity.Frontend.Media.Mvc.Models.ImageGallery;
using Telerik.Sitefinity.Frontend.Mvc.Models;
using Telerik.Sitefinity.Frontend.TestUtilities;
using Telerik.Sitefinity.Libraries.Model;
using Telerik.Sitefinity.RelatedData;
using Telerik.Sitefinity.TestUtilities.CommonOperations;
using Telerik.Sitefinity.Utilities.TypeConverters;

namespace FeatherWidgets.TestIntegration.MediaGalleryWidgets
{
    [TestFixture]
    [Description("Tests for the ability to display images related to a specific item.")]
    public class ImageGalleryRelatedDataTests
    {
        [FixtureSetUp]
        public void Setup()
        {
            ServerOperationsFeather.DynamicModules().EnsureModuleIsImported(ModuleName, ModuleResource);
            ServerOperations.DynamicTypes().CreateDynamicItem(this.GetDynamicType().FullName, "myUrl", null, "MyItemTitle");
            var manager = DynamicModuleManager.GetManager();
            var dynamicItem = this.GetDynamicItem();

            ServerOperations.Images().CreateLibrary(LibraryTitle);
            var imageId1 = ServerOperations.Images().Upload(LibraryTitle, ImagetTitle + 1, ImageResource1);
            var imageId2 = ServerOperations.Images().Upload(LibraryTitle, ImagetTitle + 2, ImageResource2);
            var imageId3 = ServerOperations.Images().Upload(LibraryTitle, ImagetTitle + 3, ImageResource3);

            dynamicItem.CreateRelation(imageId1, string.Empty, typeof(Image).FullName, RelatedImagesField);
            dynamicItem.CreateRelation(imageId2, string.Empty, typeof(Image).FullName, RelatedImagesField);
            dynamicItem.CreateRelation(imageId3, string.Empty, typeof(Image).FullName, RelatedImagesField);

            manager.SaveChanges();
        }

        [FixtureTearDown]
        public void Teardown()
        {
            DynamicModuleManager dynamicModuleManager = DynamicModuleManager.GetManager();
            Type type = this.GetDynamicType();
            dynamicModuleManager.DeleteDataItems(type);
            dynamicModuleManager.SaveChanges();

            ServerOperations.ModuleBuilder().DeleteAllModules(this.providerName, TransactionName);

            ////ServerOperations.Images().DeleteAllImages(ContentLifecycleStatus.Master);
        }

        [Test]
        [Description("Verifies that the params passed to the RelatedData action will be set to the model.")]
        [Author(FeatherTeams.FeatherTeam)]
        [Category(TestCategories.Media)]
        public void RelatedData_Action_EnsureParamsArePassedToModel()
        {
            var galleryController = new ImageGalleryController();

            var templateName = "SimpleList";
            var detailPage = Guid.NewGuid();
            var openInSamePage = false;
            var page = 2;

            var relatedData = new RelatedDataViewModel()
            {
                RelatedFieldName = "RelatedImages",
                RelatedItemProviderName = string.Empty,
                RelatedItemType = "Telerik.Sitefinity.DynamicTypes.Model.RelatedImagesModule.RelatedImagesType",
                RelationTypeToDisplay = "Child"
            };

            var settings = new ImageGallerySettingsViewModel()
            {
                EnableSocialSharing = true,
                ItemsPerPage = 10,
                ListCssClass = "sf-class",
                SortExpression = "PublicationDate DESC",
                SelectionMode = SelectionMode.FilteredItems,
                SerializedThumbnailSizeModel = @"{""thumbnail"":{""url"":null,""name"":""thumb80""},""displayMode"":""Thumbnail"",""customSize"":null}"
            };

            var view = galleryController.RelatedData(
                this.GetDynamicItem(),
                templateName,
                relatedData,
                settings,
                page,
                detailPage,
                openInSamePage);

            var viewResult = view as ViewResult;
            Assert.AreEqual("List." + templateName, viewResult.ViewName);
            Assert.AreEqual(detailPage, galleryController.DetailsPageId);
            Assert.AreEqual(openInSamePage, galleryController.OpenInSamePage);

            // The widget resolves the name of the default provider.
            var expectedProvider = "OpenAccessProvider";

            Assert.AreEqual(relatedData.RelatedFieldName, galleryController.Model.RelatedFieldName);
            Assert.AreEqual(expectedProvider, galleryController.Model.RelatedItemProviderName);
            Assert.AreEqual(relatedData.RelatedItemType, galleryController.Model.RelatedItemType);
            Assert.AreEqual(relatedData.RelationTypeToDisplay, galleryController.Model.RelationTypeToDisplay.ToString());

            Assert.AreEqual(settings.EnableSocialSharing, galleryController.Model.EnableSocialSharing);
            Assert.AreEqual(settings.ItemsPerPage, galleryController.Model.ItemsPerPage);
            Assert.AreEqual(settings.ListCssClass, galleryController.Model.ListCssClass);
            Assert.AreEqual(settings.SortExpression, galleryController.Model.SortExpression);
            Assert.AreEqual(settings.SelectionMode, galleryController.Model.SelectionMode);
            Assert.AreEqual(settings.SerializedThumbnailSizeModel, galleryController.Model.SerializedThumbnailSizeModel);
        }

        // Ignored because no relation is created between the items in the Set Up.
        [Test]
        [Description("Verifies that the related data is retrieved in the view model.")]
        [Author(FeatherTeams.FeatherTeam)]
        [Category(TestCategories.Media)]
        [Ignore]
        public void RelatedData_Action_EnsureRelatedDataIsRetrieved()
        {
            var galleryController = new ImageGalleryController();

            var relatedData = new RelatedDataViewModel()
            {
                RelatedFieldName = "RelatedImages",
                RelatedItemProviderName = string.Empty,
                RelatedItemType = "Telerik.Sitefinity.DynamicTypes.Model.RelatedImagesModule.RelatedImagesType",
                RelationTypeToDisplay = "Child"
            };

            var dynamicItem = this.GetDynamicItem();

            var view = galleryController.RelatedData(
                dynamicItem,
                null,
                relatedData,
                null,
                null,
                null,
                null);

            var viewResult = view as ViewResult;

            var viewModel = (ContentListViewModel)viewResult.Model;
            var viewModelItems = viewModel.Items.ToList();

            var relatedImages = dynamicItem.GetRelatedItems<Image>(RelatedImagesField).OrderByDescending(i => i.PublicationDate).ToList();

            Assert.AreEqual(relatedImages.Count(), viewModel.Items.Count());

            for (var i = 0; i < relatedImages.Count(); i++)
            {
                Assert.AreEqual(relatedImages[i].Id, viewModelItems[i].DataItem.Id);
            }
        }

        private DynamicContent GetDynamicItem()
        {
            var type = this.GetDynamicType();
            return DynamicModuleManager.GetManager().GetDataItems(type).First();
        }

        private Type GetDynamicType()
        {
            return TypeResolutionService.ResolveType("Telerik.Sitefinity.DynamicTypes.Model.RelatedImagesModule.RelatedImagesType");
        }

        private const string ModuleName = "RelatedImagesModule";
        private const string ModuleResource = "FeatherWidgets.TestUtilities.Data.DynamicModules.RelatedImagesModule.zip";
        private const string TransactionName = "Module Installations";

        private const string LibraryTitle = "TestimageLibrary";
        private const string ImagetTitle = "Image";
        private const string ImageResource1 = "Telerik.Sitefinity.TestUtilities.Data.Images.1.jpg";
        private const string ImageResource2 = "Telerik.Sitefinity.TestUtilities.Data.Images.2.jpg";
        private const string ImageResource3 = "Telerik.Sitefinity.TestUtilities.Data.Images.3.jpg";

        private const string RelatedImagesField = "RelatedImages";

        private string providerName = string.Empty;
    }
}
