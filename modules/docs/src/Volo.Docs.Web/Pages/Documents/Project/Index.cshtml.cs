using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Docs.Documents;
using Volo.Docs.Formatting;

namespace Volo.Docs.Pages.Documents.Project
{
    public class IndexModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string ProjectName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Version { get; set; } = "";

        [BindProperty(SupportsGet = true)]
        public string DocumentName { get; set; }

        public DocumentWithDetailsDto Document { get; private set; }

        public List<VersionInfo> Versions { get; private set; }

        public List<SelectListItem> VersionSelectItems => Versions.Select(v => new SelectListItem
        {
            Text = v.DisplayText,
            Value = "/documents/" + ProjectName + "/" + v.Version + "/" + DocumentName,
            Selected = v.IsSelected
        }).ToList();

        public NavigationWithDetailsDto Navigation { get; private set; }

        private readonly IDocumentAppService _documentAppService;
        private readonly IDocumentConverterFactory _documentConverterFactory;

        public IndexModel(IDocumentAppService documentAppService, IDocumentConverterFactory documentConverterFactory)
        {
            _documentAppService = documentAppService;
            _documentConverterFactory = documentConverterFactory;
        }

        public async Task OnGet()
        {
            Versions = (await _documentAppService.GetVersions(ProjectName, DocumentName))
                .Select(v => new VersionInfo(v, v))
                .ToList();

            var hasAnyVersion = Versions.Any();

            AddDefaultVersionIfNotContains();

            var latestVersion = hasAnyVersion ? Versions[1] : Versions[0];
            latestVersion.DisplayText = $"{latestVersion.Version} - latest";
            latestVersion.Version = latestVersion.Version;


            var versionFromUrl = Versions.FirstOrDefault(v => v.Version == Version);
            if (versionFromUrl != null)
            {
                versionFromUrl.IsSelected = true;
            }
            else if (string.Equals(Version, "latest", StringComparison.InvariantCultureIgnoreCase))
            {
                latestVersion.IsSelected = true;
            }
            else
            {
                Versions.First().IsSelected = true;
            }

            if (Version == null)
            {
                Version = Versions.Single(x => x.IsSelected).Version;
            }

            Document = await _documentAppService.GetByNameAsync(ProjectName, DocumentName, Version, true);
            var converter = _documentConverterFactory.Create(Document.Format ?? "md");
            var content = converter.NormalizeLinks(Document.Content, Document.Project.ShortName, Document.Version, Document.LocalDirectory);
            content = converter.Convert(content);

            content = HtmlNormalizer.ReplaceImageSources(content, Document.RawRootUrl, Document.LocalDirectory);
            Document.Content = content;

            Navigation = await _documentAppService.GetNavigationDocumentAsync(ProjectName, Version, false);
            Navigation.ConvertItems();
        }

        private void AddDefaultVersionIfNotContains()
        {
            if (DocsWebConsts.DefaultVersion == null)
            {
                return;
            }

            if (Versions.Contains(DocsWebConsts.DefaultVersion))
            {
                return;
            }

            Versions.Insert(0, DocsWebConsts.DefaultVersion);
        }
    }
}