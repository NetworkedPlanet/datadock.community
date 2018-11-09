var stepped = 0, chunks = 0, rows = 0, columnCount = 0;
var start, end;
var parser;
var pauseChecked = false;
var printStepChecked = false;

var filename = "";
var csvFile;
var csvData;
var header;
var columnSet;

var schemaTitle;
var templateMetadata;


$(function() {

    // event subscriptions

    $("#fileSelectTextBox").click(function(e){
        $("input:file", $(e.target).parents()).click();
    });
    
    $("#fileSelectButton").click(function(e){
        $("input:file", $(e.target).parents()).click();
    });

    // Note: DataDock specific jquery-validate configuration is in jquery.dform-1.1.0.js (line 786)

    // add types to dForm
    $.dform.subscribe("changeTab", function (options, type) {
        if (options !== "") {
            this.click(function () {
                hideAllTabContent();
                $("#" + options).show();
                $("#" + options + "Tab").addClass("active");
                return false;
            });
        }
        
    });

    $.dform.subscribe("updateDatasetId", function (options, type) {
        if (options !== "") {
            this.keyup(function () {
                var title = $("#datasetTitle").val();
                var slug = slugify(title, "", "", "camelCase");
                var datasetId = getPrefix() + "/id/dataset/" + slug;
                $("#datasetId").val(datasetId);
                return false;
            });
        }

    });

    $("#fileSelect").on("change", function() {
        clearErrors();
        stepped = 0;
        chunks = 0;
        rows = 0;

        // todo check that the file input can only select CSV
        var files = $("#fileSelect")[0].files;
        var config = buildConfig();

        pauseChecked = $("#step-pause").prop("checked");
        printStepChecked = $("#print-steps").prop("checked");


        if (files.length > 0)
        {
            var file = files[0];
            if (file.size > 1024 * 1024 * 10) {
                displaySingleError("File size is over the 4MB limit. Reduce file size before trying again.");
                return false;
            }

            start = performance.now();
            Papa.parse(file, config);
        }
        else
        {
            displaySingleError("No file found. Please try again.");
        }
    });

    $("#submit-unparse").click(function()
    {
        var input = $("#input").val();
        var delim = $("#delimiter").val();
        var header = $("#header").prop("checked");

        var results = Papa.unparse(input, {
            delimiter: delim,
            header: header
        });

        console.log("Unparse complete!");
        console.log("--------------------------------------");
        console.log(results);
        console.log("--------------------------------------");
    });

    $("#insert-tab").click(function()
    {
        $("#delimiter").val("\t");
    });

    begin();

});

function begin() {
    showLoading();
    if (schemaId) {
        loadSchemaBeforeDisplay();
    } else {
        displayFileSelector();
    }
}

function displayFileSelector() {
    if (schemaTitle) {
        $("#templateTitle").html(schemaTitle);
        $("#metadataEditorForm").addClass("info");
        $("#templateInfoMessage").show();
    }
    showStep1();
}

function constructCsvwMetadata() {
    var csvw = {};
    csvw["@context"] = "http://www.w3.org/ns/csvw";

    var datasetId = $("#datasetId").val();

    csvw["url"] = datasetId;

    csvw["dc:title"] = $("#datasetTitle").val();

    csvw["dc:description"] = $("#datasetDescription").val();

    var keywords = $("#keywords").val();
    if (keywords) {
        if (keywords.indexOf(",") < 0) {
            csvw["dcat:keyword"] = [keywords];
        } else {
            var keywordsArray = keywords.split(",");
            csvw["dcat:keyword"] = keywordsArray;
        }
    } 

    csvw["dc:license"] = $("#datasetLicense").val();

    var suffix = $("#aboutUrlSuffix").val();
    var aboutUrl = datasetId.replace("/dataset/", "/resource/") + "/" + suffix;
    csvw["aboutUrl"] = aboutUrl;

    csvw["tableSchema"] = constructCsvwtableSchema();

    console.log(csvw);
    return csvw;
}

function constructCsvwtableSchema() {
    var tableSchema = {};

    var columns = [];

    console.log(columnSet);
    if (columnSet) {
        for (var i = 0; i < columnSet.length; i++) {
            var colName = columnSet[i];
            var colId = "#" + colName;
            var skip = $(colId + "_suppress").prop("checked");
            var col = constructCsvwColumn(colName, skip);
            columns.push(col);
        }
    }
    tableSchema["columns"] = columns;

    return tableSchema;
}

function constructCsvwColumn(columnName, skip) {
    var colId = "#" + columnName;
    var datatype = $(colId + "_datatype").val();
    var column = {};
    column["name"] = columnName;
    if (skip) {
        column["suppressOutput"] = true;
    } else {

        var columnTitle = $(colId + "_title").val();
        column["titles"] = [columnTitle];

        if (datatype === "uri") {
            column["valueUrl"] = "{" + columnName + "}";
        } else {
            column["datatype"] = $(colId + "_datatype").val();
        }

        column["propertyUrl"] = $(colId + "_property_url").val();
    }
    return column;
}

function sendData(e){
    clearErrors();

    $("#step2").removeClass("active");
    $("#step3").addClass("active");
    
    var formData = new FormData();
    formData.append("ownerId", ownerId); // global variable set on Import.cshtml
    formData.append("repoId", repoId); // global variable set on Import.cshtml
    formData.append("file", csvFile, filename);
    formData.append("filename", filename);
    formData.append("metadata", JSON.stringify(constructCsvwMetadata()));
    formData.append("showOnHomePage", JSON.stringify($("#showOnHomepage").prop("checked")));
    formData.append("saveAsSchema", JSON.stringify($("#saveAsTemplate").prop("checked")));
    formData.append("addToExisting", JSON.stringify($("#addToExistingData").prop("checked")));
    
    var apiOptions ={
        url: "/api/data",
        type: "POST",
        data: formData,
        processData: false,
        contentType: false,
        success: function (r) {
            sendDataSuccess(r);
        },
        error: function (r) {co
            sendDataFailure(r);
        }
    };

    $("#metadataEditor").hide();
    $("#loading").show();
    $.ajax(apiOptions);

    return false;
}

function sendDataSuccess(response) {
    var jobsUrl = "/" + ownerId + "/" + repoId + "/jobs";
    if (baseUrl) {
        jobsUrl = baseUrl + jobsUrl;
    }
    if (response) {
        if (response["statusCode"] === 200) {
            var jobIds = response["jobIds"];
            if (jobIds) {
                jobsUrl = jobsUrl + "/" + jobIds;
            } else {
                jobsUrl = jobsUrl + "/latest";
            }
        }
        window.location.href = jobsUrl;
    } else {
        $("#warning-messages ul li:last").append("<li><span>The job has been successfully started but we cannot redirect you automatically, please check the job history page for more information on the publishing process.</span></li>");
        //todo show warnings
    }
    
}

function sendDataFailure(response) {
    $("#metadataEditor").show();
    $("#loading").hide();

    if (response) {
        var responseMsg = response["responseText"];
        displaySingleError("Publish data API has reported an error: " + responseMsg);
    } else {
        displaySingleError("Publish data API has resulted in an unspecified error.");
    }
}

//papaparse
function buildConfig()
{
    var config = {
        header: false,
        preview: 0,
        delimiter: $("#delimiter").val(),
        newline: getLineEnding(),
        comments: $("#comments").val(),
        encoding: $("#encoding").val(),
        worker: false,
        step: undefined,
        complete: completeFn,
        error: errorFn,
        download: false,
        skipEmptyLines: true,
        //chunk: $('#chunk').prop('checked') ? chunkFn : undefined,
        chunk: undefined,
        beforeFirstChunk: undefined
    };
    return config;

    function getLineEnding()
    {
        if ($("#newline-n").is(":checked"))
            return "\n";
        else if ($("#newline-r").is(":checked"))
            return "\r";
        else if ($("#newline-rn").is(":checked"))
            return "\r\n";
        else
            return "";
    }


}

function stepFn(results, parserHandle)
{
    stepped++;
    rows += results.data.length;

    parser = parserHandle;

    if (pauseChecked)
    {
        //console.log(results, results.data[0]);
        parserHandle.pause();
        return;
    }

    if (printStepChecked) {
        //console.log(results, results.data[0]);
    }
        
}

function chunkFn(results, streamer, file)
{
    if (!results)
        return;
    chunks++;
    rows += results.data.length;

    parser = streamer;

    if (printStepChecked)
        console.log("Chunk data:", results.data.length, results);

    if (pauseChecked)
    {
        console.log("Pausing; " + results.data.length + " rows in chunk; file:", file);
        streamer.pause();
        return;
    }
}

function errorFn(error, file)
{
    console.log("ERROR:", error, file);
}

function completeFn()
{
    end = performance.now();
    if (!$("#stream").prop("checked")
        && !$("#chunk").prop("checked")
        && arguments[0]
        && arguments[0].data)
        rows = arguments[0].data.length;

    csvData = arguments[0].data;
    // arguments[0] .data [][] | .errors [] | meta (aborted, cursor, delimiter, linebreak, truncated)
    var file = arguments[1];
    filename = file.name; // save in global variable
    csvFile = file;
    if (csvData) {
        header = csvData[0];
        columnCount = header.length;
    }
    // console.log("Finished input (async). Time:", end-start, arguments);
    // console.log("Rows:", rows, "Stepped:", stepped, "Chunks:", chunks);
    loadEditor();
}
//end papaparse

//jquery.dform
function loadEditor() {
    
    columnSet = [];

    var datasetInfoTabContent = constructBasicTabContent();
    var identifiersTabContent = constructIdentifiersTabContent();
    var columnDefinitionsTabContent = constructColumnDefinitionsTabContent();
    var advancedTabContent = constructAdvancedTabContent();
    var previewTabContent = constructPreviewTabContent();

    var submitButton = {
        "type": "div",
        "class": "ui center aligned container",
        "html": [
            {
                "type": "div",
                "class": "ui hidden divider",
                "html": ""
            },
            {
                "type": "div",
                "class": "ui buttons",
                "html": [
                    {
                        "type": "submit",
                        "id": "publish",
                        "class": "ui primary button large",
                        "publish": "sendData",
                        "value": "Publish Data"
                    }
                ]
            }
        ]
    };
    
    var tabs = {
        "type": "div",
        "class": "four wide column",
        "html": {
            "type": "div",
            "class": "ui vertical pointing menu",
            "html": [
                {
                    "type": "a",
                    "html": "Dataset Details",
                    "class": "item",
                    "id": "datasetInfoTab",
                    "changeTab": "datasetInfo"
                },
                {
                    "type": "a",
                    "html": "Identifiers",
                    "class": "item",
                    "id": "identifierTab",
                    "changeTab": "identifier"
                },
                {
                    "type": "a",
                    "html": "Column Definitions",
                    "class": "item",
                    "id": "columnDefinitionsTab",
                    "changeTab": "columnDefinitions"
                },
                {
                    "type": "a",
                    "html": "Advanced",
                    "class": "item",
                    "id": "advancedTab",
                    "changeTab": "advanced"
                },
                {
                    "type": "a",
                    "html": "Data Preview",
                    "class": "item",
                    "id": "previewTab",
                    "changeTab": "preview"
                }
            ]
        }
    };
    var tabsContent = {
        "type": "div",
        "class": "twelve wide stretched column",
        "html": {
            "type": "div",
            "class": "tabcontent",
            "html": [
                {
                    "type": "div",
                    "id": "datasetInfo",
                    "html": datasetInfoTabContent
                },
                {
                    "type": "div",
                    "id": "identifier",
                    "html": identifiersTabContent
                },
                {
                    "type": "div",
                    "id": "columnDefinitions",
                    "html": columnDefinitionsTabContent
                },
                {
                    "type": "div",
                    "id": "advanced",
                    "html": advancedTabContent
                },
                {
                    "type": "div",
                    "id": "preview",
                    "html": previewTabContent
                }
            ]
        }
    };


    var mainForm = {
        "type": "div",
        "class": "ui stackable two column grid container",
        "html": [tabs, tabsContent]
    };

    var configCheckboxes = constructPublishOptionsCheckboxes();

    var formTemplate = {
        "class": "ui form",
        "method": "POST"
    };
    formTemplate.html = [mainForm, configCheckboxes, submitButton];
    
    $("#metadataEditorForm").dform(formTemplate);

    // set selected license from template
    if (templateMetadata) {
        var licenseFromTemplate = getMetadataLicenseUri();
        if (licenseFromTemplate) {
            $("#datasetLicense").val(licenseFromTemplate);
        }
    }
    // set the column datatypes from the template
    setDatatypesFromTemplate();
    // set the aboutUrl from the template
    if (templateMetadata) {
        var colToUse = getMetadataIdentifierColumnName();
        var valToUse = "row_{_row}";
        if (colToUse !== "") {
            valToUse = colToUse + "/{" + colToUse + "}";
        }
        $("#aboutUrlSuffix").val(valToUse);
    }

    showStep2();

    // show first tab
    hideAllTabContent();
    $("#datasetInfo").show();
    $("#datasetInfoTab").addClass("active");

    // inputosaurus
    $("#keywords").inputosaurus({
        inputDelimiters: [",", ";"],
        width: "100%",
        change: function (ev) {
            $("#keywords_reflect").val(ev.target.value);
        }
    });

    // Prevent form submission when the user presses enter
    $("#metadataEditorForm").on("keypress", ":input:not(textarea)", function (event) {
        if (event.keyCode === 13) {
            event.preventDefault();
        }
    });
  
}

function constructBasicTabContent() {
    var datasetVoidFields = [
        {
            "type": "div",
            "class": "field",
            "html": {
                "name": "datasetTitle",
                "id": "datasetTitle",
                "caption": "Title",
                "type": "text",
                "updateDatasetId": "this",
                "value": getMetadataTitle(filename) || filename,
                "validate": {
                    "required": true,
                    "minlength": 2,
                    "messages": {
                        "required": "You must enter a title",
                        "minlength": "The title must be at least 2 characters long"
                    }
                }
            }
        },
        {
            "type": "div",
            "class": "field",
            "html": {
                "name": "datasetDescription",
                "id": "datasetDescription",
                "caption": "Description",
                "type": "textarea",
                "value": getMetadataDescription() || ""
            }
        },
        {
            "type": "div",
            "class": "field",
            "html": {
                "name": "datasetLicense",
                "id": "datasetLicense",
                "caption": "License",
                "type": "select",
                "options": {
                    "": "Please select a license",
                    "https://creativecommons.org/publicdomain/zero/1.0/": "Public Domain (CC-0)",
                    "https://creativecommons.org/licenses/by/4.0/": "Attribution (CC-BY)",
                    "https://creativecommons.org/licenses/by-sa/4.0/": "Attribution-ShareAlike (CC-BY-SA)",
                    "http://www.nationalarchives.gov.uk/doc/open-government-licence/version/3/":
                        "Open Government License (OGL)",
                    "https://opendatacommons.org/licenses/pddl/":
                        "Open Data Commons Public Domain Dedication and License (PDDL)",
                    "https://opendatacommons.org/licenses/by/": "Open Data Commons Attribution License (ODC-By)"
                },
                "validate": {
                    "required": true,
                    "messages": {
                        "required": "You must select a license"
                    }
                }
            }
        },
        {
            "type": "div",
            "class": "field",
            "html": {
                "name": "keywords",
                "id": "keywords",
                "caption": "Keywords (separate using commas)",
                "type": "text",
                "value": getMetadataTags()
            }
        }
    ];
    return datasetVoidFields;
}
function constructIdentifiersTabContent() {
    var prefix = getPrefix();
    var idFromFilename = prefix + "/id/dataset/" + slugify(filename, "", "", "camelCase");
    var defaultValue = getMetadataDatasetId(idFromFilename);

    var dsIdTable = {
        "type": "table",
        "class": "ui celled table",
        "html": [
            {
                "type": "thead",
                "html": {
                    "type": "tr",
                    "html": {
                        "type": "th",
                        "html": "Dataset Identifier (readonly)"
                    }
                }
            },
            {
                "type": "tbody",
                "html": [
                    {
                        "type": "tr",
                        "html": {
                            "type": "td",
                            "html": {
                                "type": "div",
                                "class": "field",
                                "html": {
                                    "type": "text",
                                    "readonly": true,
                                    "id": "datasetId",
                                    "name": "datasetId",
                                    "value": defaultValue,
                                    "disabled": true
                                }
                            }
                        }
                    },
                    {
                        "type": "tr",
                        "html": {
                            "type": "td",
                            "html": "The identifier for the dataset is constructed from the chosen title, the GitHub repository, and the GitHub user or organisation you're uploading the data to."
                        }
                    }
                ]
            }
        ]
    };


    var identifierTableElements = [];
    identifierTableElements.push(
        {
            "type": "thead",
            "html":
                [{
                    "type": "tr",
                    "html": [
                        {
                            "type": "th",
                            "html": "Construct individual record identifiers from which column's values?"
                        }
                    ]
                }
                ]
        }
    );
    var rowIdentifier = "row_{_row}";
    var identifierOptions = {};
    identifierOptions[rowIdentifier] = "Row Number";

    for (var colIdx = 0; colIdx < columnCount; colIdx++) {
        var colTitle = header[colIdx];
        var colName = slugify(colTitle, "_", "_", "lowercase");
        var colIdentifier = colName + "/{" + colName + "}";
        identifierOptions[colIdentifier] = colTitle;
    }
    identifierTableElements.push(
        {
            "type": "tbody",
            "html":
                [{
                    "type": "tr",
                    "html": [
                        {
                            "type": "td",
                            "html": [
                                {
                                    name: "aboutUrlSuffix",
                                    id: "aboutUrlSuffix",
                                    type: "select",
                                    placeholder: "",
                                    options: identifierOptions
                                }
                            ]
                        }
                    ]
                },
                {
                    "type": "tr",
                    "html": [
                        {
                            "type": "td",
                            "html": "You must be sure that there are no empty values in the data to use it as the basis for a record's identifier. We suggest using an ID field if you have one. If in doubt, use the default (the row number). <br />Identifiers can be crucial when it comes to linking between records of different datasets; <a href=\"http://datadock.io/docs/user-guide/selecting-an-identifier.html\" title=\"DataDock Documentation: Identifiers\" target=\"_blank\">You can read more about identifiers here (opens in new window)</a>. (TODO: Need documentation link)"
                        }
                    ]
                }
                ]
        }
    );
    var identifierTable = { "type": "table", "html": identifierTableElements, "class": "ui celled table" };
    var identifierSection = [];
    identifierSection.push(
        {
            "type": "div",
            "html":
                [dsIdTable, identifierTable]
        }
    );
    return identifierSection;
}
function constructColumnDefinitionsTabContent() {
    var columnDefinitionsTableElements = [];

    columnDefinitionsTableElements.push(
        {
            "type": "thead",
            "html":
                [{
                    "type": "tr",
                    "html": [
                        {
                            "type": "th",
                            "html": "Title"
                        },
                        {
                            "type": "th",
                            "html": "DataType"
                        }
                        ,
                        {
                            "type": "th",
                            "html": "Suppress In Output"
                        }
                    ]
                }
                ]
        }
    );
    for (var colIdx = 0; colIdx < columnCount; colIdx++) {

        var trElements = [];
        var colTitle = header[colIdx];
        var colName = slugify(colTitle, "_", "_", "lowercase");

        columnSet.push(colName);

        var colTemplate = getMetadataColumnTemplate(colName);
        var defaultTitleValue = getColumnTitle(colTemplate, colTitle);
        var titleField = {
            name: colName + "_title",
            id: colName + "_title",
            type: "text",
            placeholder: "",
            value: defaultTitleValue,
            "validate": {
                "required": true,
                "messages": {
                    "required": "Column '" + colName + "' is missing a title"
                }
            }
        };
        var tdTitle = { "type": "td", "html": titleField };
        trElements.push(tdTitle);

        var datatypeField = {
            name: colName + "_datatype",
            id: colName + "_datatype",
            type: "select",
            placeholder: "",
            options: {
                "string": "Text",
                "uri": "URI",
                "integer": "Whole Number",
                "decimal": "Decimal Number",
                "date": "Date",
                "datetime": "Data & Time",
                "boolean": "True/False"
            }
        };
        var tdDatatype = { "type": "td", "html": datatypeField };
        trElements.push(tdDatatype);

        var suppressField = {
            name: colName + "_suppress",
            id: colName + "_suppress",
            type: "checkbox",
            "class": "center aligned"
        };
        var suppressedInTemplate = getColumnSuppressed(colTemplate);
        if (suppressedInTemplate) {
            suppressField["checked"] = "checked";
        }
        var tdSuppress = { "type": "td", "html": suppressField };
        trElements.push(tdSuppress);

        var tr = { "type": "tr", "html": trElements };
        columnDefinitionsTableElements.push(tr);
    }
    var columnDefinitionsTable = { "type": "table", "html": columnDefinitionsTableElements, "class": "ui celled table" };
    return columnDefinitionsTable;
}
function constructAdvancedTabContent() {
    var predicateTableElements = [];
    predicateTableElements.push(
        {
            "type": "thead",
            "html":
                [{
                    "type": "tr",
                    "html": [
                        {
                            "type": "th",
                            "html": "Column"
                        },
                        {
                            "type": "th",
                            "html": "Property (URL)"
                        }
                    ]
                }
                ]
        }
    );
    for (var colIdx = 0; colIdx < columnCount; colIdx++) {

        var trElements = [];
        var colTitle = header[colIdx];
        var colName = slugify(colTitle, "_", "_", "lowercase");
        
        var titleDiv = {
            type: "div",
            html: colTitle
        };
        var tdTitle = { "type": "td", "html": titleDiv };
        trElements.push(tdTitle);

        var predicate = getPrefix() + "/id/definition/" + colName;
        var colTemplate = getMetadataColumnTemplate(colName);
        var defaultValue = getColumnPropertyUrl(colTemplate, predicate);
        var predicateField = {
            name: colName + "_property_url",
            id: colName + "_property_url",
            type: "text",
            placeholder: "",
            "value": defaultValue,
            "class": "pred-field",
            "validate": {
                "required": true,
                "pattern": /^https?:\/\/\S+[^#\/]$/i,
                "messages": {
                    "required": "Column '" + colName + "' is missing a property URL.",
                    "pattern": "Column '" + colName + "' must have a property URL that is a URL that does not end with a hash or slash."
                }
            }
        };
        var predDiv = { "type": "div", "class": "field", "html": predicateField };
        var tdPredicate = { "type": "td", "html": predDiv };
        trElements.push(tdPredicate);

        var tr = { "type": "tr", "html": trElements };
        predicateTableElements.push(tr);
    }
    var predicateTable = { "type": "table", "html": predicateTableElements, "class": "ui celled table" };
    var advancedSection = [];
    advancedSection.push(
        {
            "type": "div",
            "html":
                [predicateTable]
        }
    );
    return advancedSection;

}
function constructPreviewTabContent() {
    
    var ths = [];
    for (var i = 0; i < header.length; i++) {
        var th = {
            "type": "th",
            "html": header[i]
        };
        ths.push(th);
    }
    var thead = {
        "type": "thead",
        "html": {
            "type": "tr",
            html: ths
        }
    };
    var rows = [];
    var displayRowCount = 50;
    var originalRowCount = csvData.length - 1; // row 0 is header
    var originPlural = "s";
    if (originalRowCount === 1) {
        originPlural = "";
    }

    if (originalRowCount < displayRowCount) {
        displayRowCount = originalRowCount;
    }
    var displayPlural = "s";
    if (displayRowCount === 1) {
        displayPlural = "";
    }

    for (var i = 1; i < displayRowCount + 1; i++) {
        var rowData = csvData[i];
        var tds = [];
        for (var j = 0; j < rowData.length; j++) {
            var td = {
                "type": "td",
                "class": "top aligned preview",
                "html": rowData[j]
            };
            tds.push(td);
        }
        var row = {
            "type": "tr",
            "html": tds
        };
        rows.push(row);
    }
    var tbody = {
        "type": "tbody",
        "html": rows
    };

    var previewTable = {
        "type": "table",
        "class": "ui celled striped compact table",
        "html": [thead, tbody]
    };

    var infoMessage = {
        "type": "div",
        "class": "ui info message",
        "html":
            "<p>The file contains " + originalRowCount + " row" + originPlural + " of data, previewing " + displayRowCount + " row" + displayPlural + " of data below: </p>"
    };

    var container = {
        "type": "div",
        "class": "ui container data-preview",
        "style": "overflow-x: scroll;",
        "html": [infoMessage, previewTable]
    };
    return container;
}
function constructPublishOptionsCheckboxes() {

    var showOnHomepage = {
        "type": "div",
        "html": {
            "type": "div",
            "class": "ui checkbox",
            "html": {
                "type": "checkbox",
                "name": "showOnHomepage",
                "id": "showOnHomepage",
                "caption": "Include my published dataset on DataDock homepage and search",
                "value": true
            }
        }
    };
    var addToData = {
        "type": "div",
        "html": {
            "type": "div",
            "class": "ui checkbox",
            "html": {
                "type": "checkbox",
                "name": "addToExistingData",
                "id": "addToExistingData",
                "caption": "Add to existing data if dataset already exists (default is to overwrite existing data)",
                "value": false
            }
        }
    };
    var saveAsTemplate = {
        "type": "div",
        "html": {
            "type": "div",
            "class": "ui checkbox",
            "html": {
                "type": "checkbox",
                "name": "saveAsTemplate",
                "id": "saveAsTemplate",
                "caption": "Save this information as a template for future imports",
                "value": false
            }
        }
    };
    var divider = {
        "type": "div",
        "class": "ui divider",
        "html": ""
    };
    var hiddenDivider = {
        "type": "div",
        "class": "ui hidden divider",
        "html": ""
    };
    var configCheckboxes = {
        "type": "div",
        "class": "ui center aligned container",
        "html": [divider, addToData, hiddenDivider, showOnHomepage, hiddenDivider, saveAsTemplate]
    };
    return configCheckboxes;
}
//end jquery.dform

//helper functions
function getPrefix() {
    return publishUrl + "/" + ownerId + "/" + repoId;
}
function slugify(original, whitespaceReplacement, specCharReplacement, casing) {
    switch (casing)
    {
        case "lowercase":
            var lowercase = original.replace(/\s+/g, whitespaceReplacement).replace(/[^A-Z0-9]+/ig, specCharReplacement).replace("__", "_").toLowerCase();
            return lowercase;

        case "camelCase":
            var camelCase = camelize(original);
            return camelCase;

        default:
            var slug = original.replace(/\s+/g, whitespaceReplacement).replace(/[^A-Z0-9]+/ig, specCharReplacement).replace("__", "_");
            return slug;
    }
}
function camelize(str) {
    var camelised = str.split(" ").map(function (word, index) {
        if (index === 0) {
            return word.toLowerCase();
        }
        return word.charAt(0).toUpperCase() + word.slice(1).toLowerCase();
    }).join("");
    var slug = camelised.replace(/[^A-Z0-9]+/ig, "");
    return slug;
}

function isArray(value) {
    return value && typeof value === 'object' && value.constructor === Array;
}

function sniffDatatype() {

}
//end helper functions

//ui and window location functions
function hideAllTabContent() {
    $("#datasetInfo").hide();
    $("#datasetInfoTab").removeClass("active");
    $("#columnDefinitions").hide();
    $("#columnDefinitionsTab").removeClass("active");
    $("#identifier").hide();
    $("#identifierTab").removeClass("active");
    $("#advanced").hide();
    $("#advancedTab").removeClass("active");
    $("#preview").hide();
    $("#previewTab").removeClass("active");
}

function showStep1() {
    $("#fileSelector").show();
    $("#metadataEditor").hide();
    $("#loading").hide();
}

function showStep2() {
    $("#fileSelector").hide();
    $("#metadataEditor").show();
    $("#step1").removeClass("active");
    $("#step2").addClass("active");
    $("#loading").hide();
}

function showLoading() {
    $("#fileSelector").hide();
    $("#metadataEditor").hide();
    $("#loading").show();
}

function displaySingleError(error) {
    //console.error(error);
    $("#error-messages").append("<div><i class=\"warning sign icon\"></i><span>" + error + "</span></div>");
    $("#error-messages").show();  
}
function displayErrors(errors) {
    if (errors) {
        $("#error-messages").append("<div><i class=\"warning sign icon\"></i></div>");
        var list = $("<ul/>");
        $.each(errors, function (i) {
            $('<li />', { html: errors[i] }).appendTo(list);
        });
        list.appendTo("#error-messages");
    }
    $("#error-messages").show();
}

function clearErrors() {
   $("#error-messages").html("");
   $("#error-messages").hide();
}

function chooseFile() {
    var restartLocation = "/" + ownerId + "/" + repoId + "/import";
    if (baseUrl) {
        window.location.href = baseUrl + restartLocation;
    }
    window.location.href = restartLocation;
}

function setDatatypesFromTemplate() {
    if (columnSet && templateMetadata) {
        for (var i = 0; i < columnSet.length; i++) {
            var colName = columnSet[i];
            var colTemplate = getMetadataColumnTemplate(colName);
            var colDatatype = getColumnDatatype(colTemplate);
            if (colDatatype) {
                var selector = $("#" + colName + "_datatype");
                if (selector) {
                    selector.val(colDatatype);
                }
            }
        }
    }
}
//end ui functions

//schema/template functions 
function loadSchemaBeforeDisplay() {
    if (schemaId) {
        var options = {
            url: "/api/schemas",
            type: "get",
            data: {
                "ownerId": ownerId,
                "schemaId": schemaId
            },
            success: function(response) {
                console.log("Template returned from DataDock schema API");
                console.log(response);
                if (response["schema"] && response["schema"]["metadata"]) {
                    schemaTitle = response["schema"]["dc:title"] || "";
                    templateMetadata = response["schema"]["metadata"];
                } else {
                    console.error("Could not find a template in the response");
                }
                // now build form
                displayFileSelector();
            },
            error: function(response) {
                console.error("Unable to retrieve template from DataDock schema API");
                console.error(response);
                // build form without schema 
                // todo show error message about missing schema
                displayFileSelector();
            }
        };
        $.ajax(options);
    } else {
        displayFileSelector();
    }
}

//end schema/template functions

/*
$(document).ready(function () {
    if (searchUri) {
        $.getJSON(searchUri)
            .done(function (data) {
                // On success, 'data' contains a list of datasets.
                $.each(data, function (key, item) {
                    // Add a list item for the dataset.
                    $('<div>', { text: formatItem(item) }).appendTo($('#datasets'));
                });
            });
    }

});
*/

function formatResults(results, searchInput) {
    console.log(results);
    console.log(searchInput);

    var $resultsHtml = $("<div id='results-inner'></div>");
    var $hiddenDivider = $("<div/>", { 'class': "ui hidden divider" });

    var $heading = $("<h2/>", { text: "Results: " + searchInput }).appendTo($resultsHtml);
    $resultsHtml.append($hiddenDivider);

    if (results && Array.isArray(results) && results.length > 0) {
        var $cards = $("<div/>", { 'class': "ui two cards" }).appendTo($resultsHtml);

        $.each(results,
            function(key, ds) {
                var dsUrl = getMetadataValue(ds.metadata, "url", "");
                var lastMod = moment(ds.LastModified);
                var dsDesc = getMetadataValue(ds.metadata, "dc:description", "");
                var dsLicense = getMetadataValue(ds.metadata, "dc:license", "");
                console.log(ds);


                if (dsDesc !== "") {
                    dsDesc = dsDesc.replace(/^(.{20}[^\s]*).*/, "$1") + "...\n";
                }

                var $card = $("<div/>",
                    {
                        'class': "card",
                        'property': "void:subset",
                        'resource': dsUrl
                    }).appendTo($cards);
                var $cardContent = $("<div/>",
                    {
                        'class': "content",
                        about: dsUrl
                    }).appendTo($card);
                var $header = $("<div/>", { 'class': "header" })
                    .append($("<h3/>",
                        {
                            'property': 'dc:title'
                        }).append($("<a />",
                            {
                                href: dsUrl,
                                text: getMetadataValue(ds.metadata, "dc:title", ds.datasetId)
                            })
                    ));
                $header.appendTo($cardContent);
                var $details = $("<dl />").appendTo($cardContent);
                // id
                $("<dt />", { text: "Identifier" }).appendTo($details);
                $("<dd />", { text: dsUrl }).appendTo($details);
                // last modified
                $("<dt />", { text: "Last Modified" }).appendTo($details);
                $("<dd />", { text: lastMod.format("D MMM YYYY") + ' at ' + lastMod.format("HH:mm") })
                    .appendTo($details);
                // id
                $("<dt />", { text: "License" }).appendTo($details);
                $("<dd />", { text: dsLicense }).appendTo($details);
                // description
                if (dsDesc !== "") {
                    $("<dt />", { text: "Description" }).appendTo($details);
                    $("<dd />", { text: dsDesc }).appendTo($details);
                }

                var $extraContent = $("<div/>",
                    {
                        'class': "extra content"
                    }).appendTo($card);
                var $span1 = $("<span/>",
                    {
                        'class': "right floated"
                    }).appendTo($extraContent);
                var $stat = $("<div/>",
                    {
                        'class': "ui mini statistic"
                    }).appendTo($span1);
                var $val = $("<div/>",
                    {
                        'class': "value",
                        'property': "void:triple",
                        'datatype': "http://www.w3.org/2001/XMLSchema#integer",
                        text: getMetadataValue(ds.voidMetadata, "void:triples", "")
                    }).appendTo($stat);
                var $label = $("<div/>",
                    {
                        'class': "label",
                        text: "Triples"
                    }).appendTo($stat);

                var downloads = getMetadataArray(ds.voidMetadata, "void:dataDump");
                if (downloads) {
                    var len = downloads.length;
                    for (var i = 0; i < len; i++) {
                        var label = "";
                        if (downloads[i].endsWith("csv")) {
                            label = "CSV";
                        } else {
                            label = "N-QUADS";
                        }
                        var $ddlink = ($("<a />",
                            {
                                href: downloads[i],
                                'class': "ui primary button mr",
                                'property': "void:dataDump",
                                text: label
                            }).appendTo($extraContent));
                        ($("<i />", { 'class': 'download icon' }).appendTo($ddlink));
                    }
                }
            });
    } else {
        //0 results
        var $none = $("<p/>", { 'class': "ui big", text: 'No datasets found.'}).appendTo($resultsHtml);
    }
    $("#results").empty().append($resultsHtml);
   

}

function getMetadataValue(metadata, propertyName, defaultValue) {
    if (metadata) {
        var value = metadata[propertyName];
        if (value) {
            return value;
        } else {
            return defaultValue;
        }
    } else {
        return null;
    }
}

function getMetadataArray(metadata, propertyName) {
    if (metadata) {
        var value = metadata[propertyName];
        if (value) {
            return value;
        } else {
            return null;
        }
    } else {
        return null;
    }
}

function find() {
    var tags = $('#tags').val();
    if (tags) {

        var query = formatQuery(tags, true);

        if (searchUri) {
            $.getJSON(searchUri + query)
                .done(function (data) {
                    //console.log(data);
                    //console.log(JSON.stringify(data));
                    $('#toc').hide();
                    if (data) {
                        $('#results').html(formatResults(data, tags));
                    } else {
                        $('#results').html('<p class="ui big">No results found.</p>');
                    }
                    
                })
                .fail(function (jqXHR, textStatus, err) {
                    console.error(err);
                    $('#datasets').text('Error: ' + err);
                });
        } else {
            //todo do not show button or allow this to run if no search URI
        }
    } else {
        //display warning
    }
}


function buttonSearch(tags) {
    $('#tags').val(''); // clear search input
    var buttonId = "#" + tags;
    $(buttonId).toggleClass("loading");
    console.log(tags);
    if (tags) {
        var query = formatQuery(tags, true);

        if (searchUri) {
            $('#loader').toggleClass("active");
            $.getJSON(searchUri + query)
                .done(function (data) {
                    console.log(data);
                    console.log(JSON.stringify(data));
                    $('#toc').hide();
                    $('#results').html(formatResults(data, tags));
                    $('#loader').toggleClass("active");
                    $('#loader').hide();
                    $(buttonId).toggleClass("loading");
                })
                .fail(function (jqXHR, textStatus, err) {
                    console.error(err);
                    $('#datasets').text('Error: ' + err);
                });
        } else {
            console.error('No search URI defined.');
        }
    } else {
        //todo display warning
        console.log('No tags defined.')
    }
}

function formatQuery(tags, and) {
    var splitTags = tags.split(" ");
    var len = splitTags.length;
    var query = "";
    for (var i = 0; i < len; i++) {
        if (query !== "") {
            query = query + "&tag=" + splitTags[i];
        } else {
            query = "?tag=" + splitTags[i];
        }
    }
    if (query && len > 1 && and) {
        query = query + "&all=true";
    }
    console.log(query);
    return query;
}
function getMetadataDatasetId(ifNotFound) {
    if (templateMetadata) {
        var templatePrefix = templateMetadata["url"];
        if (templatePrefix) {
            return templatePrefix;
        }
    }
    return ifNotFound;
}

function getMetadataIdentifier(aboutUrlPrefix, ifNotFound) {
    if (templateMetadata) {
        var templateIdentifier = templateMetadata["aboutUrl"];
        var templatePrefix = templateMetadata["url"];
        if (templateIdentifier && templatePrefix) {
            var templateAboutUrl = templatePrefix.replace("id/dataset/", "id/resource/");
            // swap template prefix to current prefix
            var identifier = templateIdentifier.replace(templateAboutUrl, aboutUrlPrefix);
            return identifier;
        }
    }
    return ifNotFound;
}

function getMetadataIdentifierColumnName() {
    if (templateMetadata) {
        var templateIdentifier = templateMetadata["aboutUrl"];
        if (templateIdentifier) {
            try {
                // get chars between { and }
                var col = templateIdentifier.substring(templateIdentifier.lastIndexOf("{") + 1, templateIdentifier.lastIndexOf("}"));
                if (col !== "_row") {
                    // ignore _row as that is not a col
                    return col;
                }
            } catch (error) {
                return "";
            }
        }
    }
    // return blank for errors or _row, as it will fall back to _row
    return "";
}

function getMetadataTitle(ifNotFound) {
    if (templateMetadata) {
        var title = templateMetadata["dc:title"];
        return title;
    }
    return ifNotFound;
}

function getMetadataDescription() {
    if (templateMetadata) {
        var desc = templateMetadata["dc:description"];
        return desc;
    }
    return "";
}

function getMetadataLicenseUri() {
    if (templateMetadata) {
        var licenseUri = templateMetadata["dc:license"];
        return licenseUri;
    }
    return "";
}

function getMetadataTags() {
    if (templateMetadata) {
        var tags = templateMetadata["dcat:keyword"];
        return tags.join();
    }
    return "";
}

function getMetadataColumnTemplate(columnName) {
    if (templateMetadata) {
        var tableSchema = templateMetadata["tableSchema"];
        if (tableSchema) {
            var metadataColumns = tableSchema["columns"]; // array
            if (metadataColumns) {
                for (let i = 0; i < metadataColumns.length; i++) {
                    if (metadataColumns[i].name === columnName) {
                        return metadataColumns[i];
                    }
                }
            }
        }
    }
    return {};
}

function getColumnTitle(template, ifNotFound) {
    if (template) {
        var titles = template["titles"];
        if (titles) {
            // first item of array
            return titles[0];
        }
    }
    return ifNotFound;
}

function getColumnPropertyUrl(template, ifNotFound) {
    if (template) {
        var propUrl = template["propertyUrl"];
        if (propUrl) {
            return propUrl;
        }
    }
    return ifNotFound;
}

function getColumnDatatype(template) {
    if (template) {
        return template["datatype"];
    }
    return "";
}

function getColumnSuppressed(template) {
    if (template) {
        return template["suppressOutput"];
    }
    return false;
}
/**
 * Inputosaurus Text 
 *
 * Must be instantiated on an <input> element
 * Allows multiple input items. Each item is represented with a removable tag that appears to be inside the input area.
 *
 * @requires:
 *
 * 	jQuery 1.7+
 * 	jQueryUI 1.8+ Core
 *
 * @version 0.1.6
 * @author Dan Kielp <dan@sproutsocial.com>
 * @created October 3,2012
 *
 */


(function($) {

	var inputosaurustext = {

		version: "0.1.6",

		eventprefix: "inputosaurus",

		options: {

			// bindable events
			//
			// 'change' - triggered whenever a tag is added or removed (should be similar to binding the the change event of the instantiated input
			// 'keyup' - keyup event on the newly created input
			
			// while typing, the user can separate values using these delimiters
			// the value tags are created on the fly when an inputDelimiter is detected
			inputDelimiters : [',', ';'],

			// this separator is used to rejoin all input items back to the value of the original <input>
			outputDelimiter : ',',

			allowDuplicates : false,

			parseOnBlur : false,

			// optional wrapper for widget
			wrapperElement : null,

			width : null,

			// simply passing an autoComplete source (array, string or function) will instantiate autocomplete functionality
			autoCompleteSource : '',

			// When forcing users to select from the autocomplete list, allow them to press 'Enter' to select an item if it's the only option left.
			activateFinalResult : false,

			// manipulate and return the input value after parseInput() parsing
			// the array of tag names is passed and expected to be returned as an array after manipulation
			parseHook : null,
			
			// define a placeholder to display when the input is empty
			placeholder: null,
			
			// when you check for duplicates it check for the case
			caseSensitiveDuplicates: false
		},

		_create: function() {
			var widget = this,
				els = {},
				o = widget.options,
				placeholder =  o.placeholder || this.element.attr('placeholder') || null;
				
			this._chosenValues = [];

			// Create the elements
			els.ul = $('<ul class="inputosaurus-container">');
			els.input = $('<input type="text" class="skip-validation" />');
			els.inputCont = $('<li class="inputosaurus-input inputosaurus-required"></li>');
			els.origInputCont = $('<li class="inputosaurus-input-hidden inputosaurus-required">');
			
			// define starting placeholder
			if (placeholder) { 
				o.placeholder = placeholder;
				els.input.attr('placeholder', o.placeholder); 
				if (o.width) {
					els.input.css('min-width', o.width - 50);
				}
			}

			o.wrapperElement && o.wrapperElement.append(els.ul);
			this.element.replaceWith(o.wrapperElement || els.ul);
			els.origInputCont.append(this.element).hide();
			
			els.inputCont.append(els.input);
			els.ul.append(els.inputCont);
			els.ul.append(els.origInputCont);
			
			o.width && els.ul.css('width', o.width);

			this.elements = els;

			widget._attachEvents();

			// if instantiated input already contains a value, parse that junk
			if($.trim(this.element.val())){
				els.input.val( this.element.val() );
				this.parseInput();
			}

			this._instAutocomplete();
		},

		_instAutocomplete : function() {
			if(this.options.autoCompleteSource){
				var widget = this;

				this.elements.input.autocomplete({
					position : {
						of : this.elements.ul
					},
					source : this.options.autoCompleteSource,
					minLength : 1,
					select : function(ev, ui){
						ev.preventDefault();
						widget.elements.input.val(ui.item.value);
						widget.parseInput();
					},
					open : function() {
						// Older versions of jQueryUI have a different namespace
						var auto =  $(this).data('ui-autocomplete') || $(this).data('autocomplete');
						var menu = auto.menu,
							$menuItems;
						
						
						// zIndex will force the element on top of anything (like a dialog it's in)
						menu.element.zIndex && menu.element.zIndex($(this).zIndex() + 1);
						menu.element.width(widget.elements.ul.outerWidth());

						// auto-activate the result if it's the only one
						if(widget.options.activateFinalResult){
							$menuItems = menu.element.find('li');

							// activate single item to allow selection upon pressing 'Enter'
							if($menuItems.size() === 1){
								menu[menu.activate ? 'activate' : 'focus']($.Event('click'), $menuItems);
							}
						}
					}
				});
			}
		},

		_autoCompleteMenuPosition : function() {
			var widget;
			if(this.options.autoCompleteSource){
				widget = this.elements.input.data('ui-autocomplete') || this.elements.input.data('autocomplete');
				widget && widget.menu.element.position({
					of: this.elements.ul,
					my: 'left top',
					at: 'left bottom',
					collision: 'none'
				});
			}
		},

		/*_closeAutoCompleteMenu : function() {
			if(this.options.autoCompleteSource){
				this.elements.input.autocomplete('close');
			}
		},*/

		parseInput : function(ev) {
			var widget = (ev && ev.data.widget) || this,
				val,
				delimiterFound = false,
				values = [];

			val = widget.elements.input.val();

			val && (delimiterFound = widget._containsDelimiter(val));

			if(delimiterFound !== false){
				values = val.split(delimiterFound);
			} else if(!ev || ev.which === $.ui.keyCode.ENTER && !$('.ui-menu-item.ui-state-focus').size() && !$('.ui-menu-item .ui-state-focus').size() && !$('#ui-active-menuitem').size()){
				values.push(val);
				ev && ev.preventDefault();

			// prevent autoComplete menu click from causing a false 'blur'
			} else if(ev.type === 'blur' && !$('#ui-active-menuitem').size()){
				values.push(val);
			}

			$.isFunction(widget.options.parseHook) && (values = widget.options.parseHook(values));

			if(values.length){
				widget._setChosen(values);
				widget.elements.input.val('');
				widget._resizeInput();
			}

			widget._resetPlaceholder();
		},

		_inputFocus : function(ev) {
			var widget = ev.data.widget || this;

			widget.elements.input.value || (widget.options.autoCompleteSource.length && widget.elements.input.autocomplete('search', ''));
		},

		_inputKeypress : function(ev) {
			var widget = ev.data.widget || this;

			ev.type === 'keyup' && widget._trigger('keyup', ev, widget);

			switch(ev.which){
				case $.ui.keyCode.BACKSPACE:
					ev.type === 'keydown' && widget._inputBackspace(ev);
					break;

				case $.ui.keyCode.LEFT:
					ev.type === 'keydown' && widget._inputBackspace(ev);
					break;

				default :
					widget.parseInput(ev);
					widget._resizeInput(ev);
			}

			// reposition autoComplete menu as <ul> grows and shrinks vertically
			if(widget.options.autoCompleteSource){
				setTimeout(function(){widget._autoCompleteMenuPosition.call(widget);}, 200);
			}
		},

		// the input dynamically resizes based on the length of its value
		_resizeInput : function(ev) {
			var widget = (ev && ev.data.widget) || this,
				maxWidth = widget.elements.ul.width(),
				val = widget.elements.input.val(),
				txtWidth = 25 + val.length * 8;

			widget.elements.input.width(txtWidth < maxWidth ? txtWidth : maxWidth);
		},
		
		// resets placeholder on representative input
		_resetPlaceholder: function () {
			var placeholder = this.options.placeholder,
				input = this.elements.input,
				width = this.options.width || 'inherit';
			if (placeholder && this.element.val().length === 0) {
				input.attr('placeholder', placeholder).css('min-width', width - 50)
			}else {
				input.attr('placeholder', '').css('min-width', 'inherit')
			}
		},

		// if our input contains no value and backspace has been pressed, select the last tag
		_inputBackspace : function(ev) {
			var widget = (ev && ev.data.widget) || this;
				lastTag = widget.elements.ul.find('li:not(.inputosaurus-required):last');

			// IE goes back in history if the event isn't stopped
			ev.stopPropagation();

			if((!$(ev.currentTarget).val() || (('selectionStart' in ev.currentTarget) && ev.currentTarget.selectionStart === 0 && ev.currentTarget.selectionEnd === 0)) && lastTag.size()){
				ev.preventDefault();
				lastTag.find('a').focus();
			}
			
		},

		_editTag : function(ev) {
			var widget = (ev && ev.data.widget) || this,
				tagName = '',
				$closest = $(ev.currentTarget).closest('li'),
				tagKey = $closest.data('ui-inputosaurus') ||  $closest.data('inputosaurus');

			if(!tagKey){
				return true;
			}

			ev.preventDefault();

			$.each(widget._chosenValues, function(i,v) {
				v.key === tagKey && (tagName = v.value);
			});

			widget.elements.input.val(tagName);

			widget._removeTag(ev);
			widget._resizeInput(ev);
		},

		_tagKeypress : function(ev) {
			var widget = ev.data.widget;
			switch(ev.which){

				case $.ui.keyCode.BACKSPACE: 
					ev && ev.preventDefault();
					ev && ev.stopPropagation();
					$(ev.currentTarget).trigger('click');
					break;

				// 'e' - edit tag (removes tag and places value into visible input
				case 69:
					widget._editTag(ev);
					break;

				case $.ui.keyCode.LEFT:
					ev.type === 'keydown' && widget._prevTag(ev);
					break;

				case $.ui.keyCode.RIGHT:
					ev.type === 'keydown' && widget._nextTag(ev);
					break;

				case $.ui.keyCode.DOWN:
					ev.type === 'keydown' && widget._focus(ev);
					break;
			}
		},

		// select the previous tag or input if no more tags exist
		_prevTag : function(ev) {
			var widget = (ev && ev.data.widget) || this,
				tag = $(ev.currentTarget).closest('li'),
				previous = tag.prev();

			if(previous.is('li')){
				previous.find('a').focus();
			} else {
				widget._focus();
			}
		},

		// select the next tag or input if no more tags exist
		_nextTag : function(ev) {
			var widget = (ev && ev.data.widget) || this,
				tag = $(ev.currentTarget).closest('li'),
				next = tag.next();

			if(next.is('li:not(.inputosaurus-input)')){
				next.find('a').focus();
			} else {
				widget._focus();
			}
		},

		// return the inputDelimiter that was detected or false if none were found
		_containsDelimiter : function(tagStr) {

			var found = false;

			$.each(this.options.inputDelimiters, function(k,v) {
				if(tagStr.indexOf(v) !== -1){
					found = v;
				}
			});

			return found;
		},

		_setChosen : function(valArr) {
			var self = this;

			if(!$.isArray(valArr)){
				return false;
			}

			$.each(valArr, function(k,v) {
				var exists = false,
					obj = {
						key : '',
						value : ''
					};

				v = $.trim(v);

				$.each(self._chosenValues, function(kk,vv) {
					if(!self.options.caseSensitiveDuplicates){
						vv.value.toLowerCase() === v.toLowerCase() && (exists = true);
					}
					else{
						vv.value === v && (exists = true);
					}
				});

				if(v !== '' && (!exists || self.options.allowDuplicates)){

					obj.key = 'mi_' + Math.random().toString( 16 ).slice( 2, 10 );
					obj.value = v;
					self._chosenValues.push(obj);

					self._renderTags();
				}
			});
			self._setValue(self._buildValue());
		},

		_buildValue : function() {
			var widget = this,
				value = '';

			$.each(this._chosenValues, function(k,v) {
				value +=  value.length ? widget.options.outputDelimiter + v.value : v.value;
			});

			return value;
		},

		_setValue : function(value) {
			var val = this.element.val();

			if(val !== value){
				this.element.val(value);
				this._trigger('change');
			}
		},

		// @name text for tag
		// @className optional className for <li>
		_createTag : function(name, key, className) {
			className = className ? ' class="' + className + '"' : '';

			if(name !== undefined){
				return $('<li' + className + ' data-inputosaurus="' + key + '"><span>' + name + '</span> <a href="javascript:void(0);" class="ficon">&#x2716;</a></li>');
			}
		},

		_renderTags : function() {
			var self = this;

			this.elements.ul.find('li:not(.inputosaurus-required)').remove();

			$.each(this._chosenValues, function(k,v) {
				var el = self._createTag(v.value, v.key);
				self.elements.ul.find('li.inputosaurus-input').before(el);
			});
		},

		_removeTag : function(ev) {
			var $closest = $(ev.currentTarget).closest('li'), 
				key = $closest.data('ui-inputosaurus') || $closest.data('inputosaurus'),
				indexFound = false,
				widget = (ev && ev.data.widget) || this;


			$.each(widget._chosenValues, function(k,v) {
				if(key === v.key){
					indexFound = k;
				}
			});

			indexFound !== false && widget._chosenValues.splice(indexFound, 1);

			widget._setValue(widget._buildValue());

			$(ev.currentTarget).closest('li').remove();
			widget.elements.input.focus();
		},

		_focus : function(ev) {
			var widget = (ev && ev.data.widget) || this,
				$closest = $(ev.target).closest('li'),
				$data = $closest.data('ui-inputosaurus') || $closest.data('inputosaurus');

			if(!ev || !$data){
				widget.elements.input.focus();
			}
		},

		_tagFocus : function(ev) {
			$(ev.currentTarget).parent()[ev.type === 'focusout' ? 'removeClass' : 'addClass']('inputosaurus-selected');
		},

		refresh : function() {
			var delim = this.options.outputDelimiter,
				val = this.element.val(),
				values = [];
			
			values.push(val);
			delim && (values = val.split(delim));

			if(values.length){
				this._chosenValues = [];

				$.isFunction(this.options.parseHook) && (values = this.options.parseHook(values));

				this._setChosen(values);
				this._renderTags();
				this.elements.input.val('');
				this._resizeInput();
			}
		},

		_attachEvents : function() {
			var widget = this;

			this.elements.input.on('keyup.inputosaurus', {widget : widget}, this._inputKeypress);
			this.elements.input.on('keydown.inputosaurus', {widget : widget}, this._inputKeypress);
			this.elements.input.on('change.inputosaurus', {widget : widget}, this._inputKeypress);
			this.elements.input.on('focus.inputosaurus', {widget : widget}, this._inputFocus);
			this.options.parseOnBlur && this.elements.input.on('blur.inputosaurus', {widget : widget}, this.parseInput);

			this.elements.ul.on('click.inputosaurus', {widget : widget}, this._focus);
			this.elements.ul.on('click.inputosaurus', 'a', {widget : widget}, this._removeTag);
			this.elements.ul.on('dblclick.inputosaurus', 'li', {widget : widget}, this._editTag);
			this.elements.ul.on('focus.inputosaurus', 'a', {widget : widget}, this._tagFocus);
			this.elements.ul.on('blur.inputosaurus', 'a', {widget : widget}, this._tagFocus);
			this.elements.ul.on('keydown.inputosaurus', 'a', {widget : widget}, this._tagKeypress);
		},

		_destroy: function() {
			this.elements.input.unbind('.inputosaurus');

			this.elements.ul.replaceWith(this.element);

		}
	};

	$.widget("ui.inputosaurus", inputosaurustext);
})(jQuery);


/*
 * jQuery dform plugin
 * Copyright (C) 2012 David Luecke <daff@neyeon.com>, [http://daffl.github.com/jquery.dform]
 * 
 * Licensed under the MIT license
 */
(function ($) {
	var _subscriptions = {},
		_types = {},
		each = $.each,
		addToObject = function (obj) {
			var result = function (data, fn, condition) {
				if (typeof data === 'object') {
					$.each(data, function (name, val) {
						result(name, val, condition);
					});
				} else if (condition === undefined || condition === true) {
					if (!obj[data]) {
						obj[data] = [];
					}
					obj[data].push(fn);
				}
			}
			return result;
		},
		isArray = $.isArray,
		/**
		 * Returns an array of keys (properties) contained in the given object.
		 *
		 * @param {Object} object The object to use
		 * @return {Array} An array containing all properties in the object
		 */
		keyset = function (object) {
			return $.map(object, function (val, key) {
				return key;
			});
		},
		/**
		 * Returns an object that contains all values from the given
		 * object that have a key which is also in the array keys.
		 *
		 * @param {Object} object The object to traverse
		 * @param {Array} keys The keys the new object should contain
		 * @return {Object} A new object containing only the properties
		 * with names given in keys
		 */
		withKeys = function (object, keys) {
			var result = {};
			each(keys, function (index, value) {
				if (object[value]) {
					result[value] = object[value];
				}
			});
			return result;
		},
		/**
		 * Returns an object that contains all value from the given
		 * object that do not have a key which is also in the array keys.
		 *
		 * @param {Object} object The object to traverse
		 * @param {Array} keys A list of keys that should not be contained in the new object
		 * @return {Object} A new object with all properties of the given object, except
		 * for the ones given in the list of keys
		 */
		withoutKeys = function (object, keys) {
			var result = {};
			each(object, function (index, value) {
				if (!~$.inArray(index, keys)) {
					result[index] = value;
				}
			});
			return result;
		},
		/**
		 * Run all subscriptions with the given name and options
		 * on an element.
		 *
		 * @param {String} name The name of the subscriber function
		 * @param {Object} options ptions for the function
		 * @param {String} type The type of the current element as in the registered types
		 * @return {Object} The jQuery object
		 */
		runSubscription = function (name, options, type) {
			if ($.dform.hasSubscription(name)) {
				this.each(function () {
					var element = $(this);
					each(_subscriptions[name], function (i, sfn) {
						// run subscriber function with options
						sfn.call(element, options, type);
					});
				});
			}
			return this;
		},
		/**
		 * Run all subscription functions with given options.
		 *
		 * @param {Object} options The options to use
		 * @return {Object} The jQuery element this function has been called on
		 */
		runAll = function (options) {
			var type = options.type, self = this;
			// Run preprocessing subscribers
			this.dform('run', '[pre]', options, type);
			each(options, function (name, sopts) {
				self.dform('run', name, sopts, type);
			});
			// Run post processing subscribers
			this.dform('run', '[post]', options, type);
			return this;
		};

	/**
	 * Globals added directly to the jQuery object
	 */
	$.extend($, {
		keyset : keyset,
		withKeys : withKeys,
		withoutKeys : withoutKeys,
		dform : {
			/**
			 * Default options the plugin is initialized with:
			 *
			 * ## prefix
			 *
			 * The Default prefix used for element classnames generated by the dform plugin.
			 * Defaults to _ui-dform-_
			 * E.g. an element with type text will have the class ui-dform-text
			 *
			 */
			options : {
				prefix : "ui-dform-"
			},

			/**
			 * A function that is called, when no registered type has been found.
			 * The default behaviour returns an HTML element with the tag
			 * as specified in type and the HTML attributes given in options
			 * (without subscriber options).
			 *
			 * @param {Object} options
			 * @return {Object} The created object
			 */
			defaultType : function (options) {
				return $("<" + options.type + ">").dform('attr', options);
			},
			/**
			 * Return all types.
			 *
			 * @params {String} name (optional) If passed return
			 * all type generators for a given name.
			 * @return {Object} Mapping from type name to
			 * an array of generator functions.
			 */
			types : function (name) {
				return name ? _types[name ] : _types;
			},
			/**
			 * Register an element type function.
			 *
			 * @param {String|Array} data Can either be the name of the type
			 * function or an object that contains name : type function pairs
			 * @param {Function} fn The function that creates a new type element
			 */
			addType : addToObject(_types),
			/**
			 * Returns all subscribers or all subscribers for a given name.
			 *
			 * @params {String} name (optional) If passed return all
			 * subscribers for a given name
			 * @return {Object} Mapping from subscriber names
			 * to an array of subscriber functions.
			 */
			subscribers : function (name) {
				return name ? _subscriptions[name] : _subscriptions;
			},
			/**
			 * Register a subscriber function.
			 *
			 * @param {String|Object} data Can either be the name of the subscriber
			 * function or an object that contains name : subscriber function pairs
			 * @param {Function} fn The function to subscribe or nothing if an object is passed for data
			 * @param {Array} deps An optional list of dependencies
			 */
			subscribe : addToObject(_subscriptions),
			/**
			 * Returns if a subscriber function with the given name
			 * has been registered.
			 *
			 * @param {String} name The subscriber name
			 * @return {Boolean} True if the given name has at least one subscriber registered,
			 *     false otherwise
			 */
			hasSubscription : function (name) {
				return _subscriptions[name] ? true : false;
			},
			/**
			 * Create a new element.
			 *
			 * @param {Object} options - The options to use
			 * @return {Object} The element as created by the builder function specified
			 *     or returned by the defaultType function.
			 */
			createElement : function (options) {
				if (!options.type) {
					throw "No element type given! Must always exist.";
				}
				var type = options.type,
					element = null,
				// We don't need the type key in the options
					opts = $.withoutKeys(options, ["type"]);

				if (_types[type]) {
					// Run all type element builder functions called typename
					each(_types[type], function (i, sfn) {
						element = sfn.call(element, opts);
					});
				} else {
					// Call defaultType function if no type was found
					element = $.dform.defaultType(options);
				}
				return $(element);
			},
			methods : {
				/**
				 * Run all subscriptions with the given name and options
				 * on an element.
				 *
				 * @param {String} name The name of the subscriber function
				 * @param {Object} options ptions for the function
				 * @param {String} type The type of the current element as in the registered types
				 * @return {Object} The jQuery object
				 */
				run : function (name, options, type) {
					if (typeof name !== 'string') {
						return runAll.call(this, name);
					}
					return runSubscription.call(this, name, options, type);
				},
				/**
				 * Creates a form element on an element with given options
				 *
				 * @param {Object} options The options to use
				 * @return {Object} The jQuery element this function has been called on
				 */
				append : function (options, converter) {
					if (converter && $.dform.converters && $.isFunction($.dform.converters[converter])) {
						options = $.dform.converters[converter](options);
					}
					// Create element (run builder function for type)
					var element = $.dform.createElement(options);
					this.append(element);
					// Run all subscriptions
					element.dform('run', options);
				},
				/**
				 * Adds HTML attributes to the current element from the given options.
				 * Any subscriber will be omitted so that the attributes will contain any
				 * key value pair where the key is not the name of a subscriber function
				 * and is not in the string array excludes.
				 *
				 * @param {Object} object The attribute object
				 * @param {Array} excludes A list of keys that should also be excluded
				 * @return {Object} The jQuery object of the this reference
				 */
				attr : function (object, excludes) {
					// Ignore any subscriber name and the objects given in excludes
					var ignores = $.keyset(_subscriptions);
					isArray(excludes) && $.merge(ignores, excludes);
					this.attr($.withoutKeys(object, ignores));
				},
				/**
				 *
				 *
				 * @param params
				 * @param success
				 * @param error
				 */
				ajax : function (params, success, error) {
					var options = {
						error : error,
						url : params
					}, self = this;
					if (typeof params !== 'string') {
						$.extend(options, params);
					}
					options.success = function (data) {
						var callback = success || params.success;
						self.dform(data);
						if(callback) {
							callback.call(self, data);
						}
					}
					$.ajax(options);
				},
				/**
				 *
				 *
				 * @param options
				 */
				init : function (options, converter) {
					var opts = options.type ? options : $.extend({ "type" : "form" }, options);
					if (converter && $.dform.converters && $.isFunction($.dform.converters[converter])) {
						opts = $.dform.converters[converter](opts);
					}
					if (this.is(opts.type)) {
						this.dform('attr', opts);
						this.dform('run', opts);
					} else {
						this.dform('append', opts);
					}
				}
			}
		}
	});

	/**
	 * The jQuery plugin function
	 *
	 * @param options The form options
	 * @param {String} converter The name of the converter in $.dform.converters
	 * that will be used to convert the options
	 */
	$.fn.dform = function (options, converter, error) {
		var self = $(this);
		if ($.dform.methods[options]) {
			$.dform.methods[options].apply(self, Array.prototype.slice.call(arguments, 1));
		} else {
			if (typeof options === 'string') {
				$.dform.methods.ajax.call(self, {
					url : options,
					dataType : 'json'
				}, converter, error);
			} else {
				$.dform.methods.init.apply(self, arguments);
			}
		}
		return this;
	}
})(jQuery);

/*
 * jQuery dform plugin
 * Copyright (C) 2012 David Luecke <daff@neyeon.com>, [http://daffl.github.com/jquery.dform]
 *
 * Licensed under the MIT license
 */
(function ($) {
	var each = $.each,
		_element = function (tag, excludes) {
			return function (ops) {
				return $(tag).dform('attr', ops, excludes);
			};
		},
		_html = function (options, type) {
			var self = this;
			if ($.isPlainObject(options)) {
				self.dform('append', options);
			} else if ($.isArray(options)) {
				each(options, function (index, nested) {
					self.dform('append', nested);
				});
			} else {
				self.html(options);
			}
		};

	$.dform.addType({
		container : _element("<div>"),
		text : _element('<input type="text" />'),
		password : _element('<input type="password" />'),
		submit : _element('<input type="submit" />'),
		reset : _element('<input type="reset" />'),
		hidden : _element('<input type="hidden" />'),
		radio : _element('<input type="radio" />'),
		checkbox : _element('<input type="checkbox" />'),
		file : _element('<input type="file" />'),
		number : _element('<input type="number" />'),
		url : _element('<input type="url" />'),
		tel : _element('<input type="tel" />'),
		email : _element('<input type="email" />'),
		checkboxes : _element("<div>", ["name"]),
		radiobuttons : _element("<div>", ["name"])
	});

	$.dform.subscribe({
		/**
		 * Adds a class to the current element.
		 * Ovverrides the default behaviour which would be replacing the class attribute.
		 *
		 * @param options A list of whitespace separated classnames
		 * @param type The type of the *this* element
		 */
		"class" : function (options, type) {
			this.addClass(options);
		},

		/**
		 * Sets html content of the current element
		 *
		 * @param options The html content to set as a string
		 * @param type The type of the *this* element
		 */
		"html" : _html,

		/**
		 * Recursively appends subelements to the current form element.
		 *
		 * @param options Either an object with key value pairs
		 *	 where the key is the element name and the value the
		 *	 subelement options or an array of objects where each object
		 *	 is the options for a subelement
		 * @param type The type of the *this* element
		 */
		"elements" : _html,

		/**
		 * Sets the value of the current element.
		 *
		 * @param options The value to set
		 * @param type The type of the *this* element
		 */
		"value" : function (options) {
			this.val(options);
		},

		/**
		 * Set CSS styles for the current element
		 *
		 * @param options The Styles to set
		 * @param type The type of the *this* element
		 */
		"css" : function (options) {
			this.css(options);
		},

		/**
		 * Adds options to select type elements or radio and checkbox list elements.
		 *
		 * @param options A key value pair where the key is the
		 *	 option value and the value the options text or the settings for the element.
		 * @param type The type of the *this* element
		 */
		"options" : function (options, type) {
			var self = this;
			// Options for select elements
			if ((type === "select" || type === "optgroup") && typeof options !== 'string')
			{
				each(options, function (value, content) {
					var option = { type : 'option', value : value };
					if (typeof (content) === "string") {
						option.html = content;
					}
					if (typeof (content) === "object") {
						option = $.extend(option, content);
					}
					self.dform('append', option);
				});
			}
			else if (type === "checkboxes" || type === "radiobuttons") {
				// Options for checkbox and radiobutton lists
				each(options, function (value, content) {
					var boxoptions = ((type === "radiobuttons") ? { "type" : "radio" } : { "type" : "checkbox" });
					if (typeof(content) === "string") {
						boxoptions["caption"] = content;
					} else {
						$.extend(boxoptions, content);
					}
					boxoptions["value"] = value;
					self.dform('append', boxoptions);
				});
			}
		},

		/**
		 * Adds caption to elements.
		 *
		 * Depending on the element type the following elements will
		 * be used:
		 * - A legend for <fieldset> elements
		 * - A <label> next to <radio> or <checkbox> elements
		 * - A <label> before any other element
		 *
		 * @param options A string for the caption or the options for the
		 * @param type The type of the *this* element
		 */
		"caption" : function (options, type) {
			var ops = {};
			if (typeof (options) === "string") {
				ops["html"] = options;
			} else {
				$.extend(ops, options);
			}

			if (type == "fieldset") {
				// Labels for fieldsets are legend
				ops.type = "legend";
				this.dform('append', ops);
			} else {
				ops.type = "label";
				if (this.attr("id")) {
					ops["for"] = this.attr("id");
				}
				var label = $($.dform.createElement(ops));
				if (type === "checkbox" || type === "radio") {
					this.parent().append($(label));
				} else {
					label.insertBefore(this);
				}
				label.dform('run', ops);
			}
		},

		/**
		 * The subscriber for the type parameter.
		 * Although the type parameter is used to get the correct element
		 * type it is just treated as a simple subscriber otherwise.
		 * Since every element needs a type
		 * parameter feel free to add other type subscribers to do
		 * any processing between [pre] and [post].
		 *
		 * This subscriber adds the auto generated classes according
		 * to the type prefix in $.dform.options.prefix.
		 *
		 * @param options The name of the type
		 * @param type The type of the *this* element
		 */
		"type" : function (options, type) {
			if ($.dform.options.prefix) {
				this.addClass($.dform.options.prefix + type);
			}
		},
		/**
		 * Retrieves JSON data from a URL and creates a sub form.
		 *
		 * @param options
		 * @param type
		 */
		"url" : function (options) {
			this.dform('ajax', options);
		},
		/**
		 * Post processing function, that will run whenever all other subscribers are finished.
		 *
		 * @param options All options that have been used for
		 * @param type The type of the *this* element
		 */
		"[post]" : function (options, type) {
			if (type === "checkboxes" || type === "radiobuttons") {
				var boxtype = ((type === "checkboxes") ? "checkbox" : "radio");
				this.children("[type=" + boxtype + "]").each(function () {
					$(this).attr("name", options.name);
				});
			}
		}
	});
})(jQuery);

/*
 * jQuery dform plugin
 * Copyright (C) 2012 David Luecke <daff@neyeon.com>, [http://daffl.github.com/jquery.dform]
 * 
 * Licensed under the MIT license
 */
(function($)
{
	var _getOptions = function(type, options)
		{
			return $.withKeys(options, $.keyset($.ui[type]["prototype"]["options"]));
		},
		_get = function(keys, obj) {
			for(var item = obj, i = 0; i < keys.length; i++) {
				item = item[keys[i]];
				if(!item) {
					return null;
				}
			}
			return item;
		}
		
	$.dform.addType("progressbar",
		/**
		 * Returns a jQuery UI progressbar.
		 *
		 * @param options  As specified in the jQuery UI progressbar documentation at
		 * 	http://jqueryui.com/demos/progressbar/
		 */
		function(options)
		{
			return $("<div>").dform('attr', options).progressbar(_getOptions("progressbar", options));
		}, $.isFunction($.fn.progressbar));

	$.dform.addType("slider",
		/**
		 * Returns a slider element.
		 *
		 * @param options As specified in the jQuery UI slider documentation at
		 * 	http://jqueryui.com/demos/slider/
		 */
		function(options)
		{
			return $("<div>").dform('attr', options).slider(_getOptions("slider", options));
		}, $.isFunction($.fn.slider));

	$.dform.addType("accordion",
		/**
		 * Creates an element container for a jQuery UI accordion.
		 *
		 * @param options As specified in the jQuery UI accordion documentation at
		 * 	http://jqueryui.com/demos/accordion/
		 */
		function(options)
		{
			return $("<div>").dform('attr', options);
		}, $.isFunction($.fn.accordion));

	$.dform.addType("tabs",
		/**
		 * Returns a container for jQuery UI tabs.
		 *
		 * @param options The options as in jQuery UI tab
		 */
		function(options)
		{
			return $("<div>").dform('attr', options);
		}, $.isFunction($.fn.tabs));
	
	$.dform.subscribe("entries",
		/**
		 *  Create entries for the accordion type.
		 *  Use the <elements> subscriber to create subelements in each entry.
		 *
		 * @param options All options for the container div. The <caption> will be
		 * 	turned into the accordion or tab title.
		 * @param type The type. This subscriber will only run for accordion
		 */
		function(options, type) {
			if(type == "accordion")
			{
				var scoper = this;
				$.each(options, function(index, options) {
					var el = $.extend({ "type" : "div" }, options);
					$(scoper).dform('append', el);
					if(options.caption) {
						var label = $(scoper).children("div:last").prev();
						label.replaceWith('<h3><a href="#">' + label.html() + '</a></h3>');
					}
				});
			}
		}, $.isFunction($.fn.accordion));

	$.dform.subscribe("entries",
		/**
		 *  Create entries for the accordion type.
		 *  Use the <elements> subscriber to create subelements in each entry.
		 *
		 * @param options All options for the container div. The <caption> will be
		 * 	turned into the accordion or tab title.
		 * @param type The type. This subscriber will only run for accordion
		 */
		function(options, type) {
			if(type == "tabs")
			{
				var scoper = this;
				this.append("<ul>");
				var ul = $(scoper).children("ul:first");
				$.each(options, function(index, options) {
					var id = options.id ? options.id : index;
					$.extend(options, { "type" : "container", "id" : id });
					$(scoper).dform('append', options);
					var label = $(scoper).children("div:last").prev();
					$(label).wrapInner($("<a>").attr("href", "#" + id));
					$(ul).append($("<li>").wrapInner(label));
				});
			}
		}, $.isFunction($.fn.tabs));
		
	$.dform.subscribe("dialog",
		/**
		 * Turns an element into a jQuery UI dialog.
		 *
		 * @param options As specified in the [jQuery UI dialog documentation\(http://jqueryui.com/demos/dialog/)
		 */
		function(options)
		{
			this.dialog(options);
		}, $.isFunction($.fn.dialog));

	$.dform.subscribe("resizable",
		/**
		 * Make the current element resizable.
		 *
		 * @param options As specified in the [jQuery UI resizable documentation](http://jqueryui.com/demos/resizable/)
		 */
		function(options)
		{
			this.resizable(options);
		}, $.isFunction($.fn.resizable));

	$.dform.subscribe("datepicker",
		/**
		 * Adds a jQuery UI datepicker to an element of type text.
		 *
		 * @param options As specified in the [jQuery UI datepicker documentation](http://jqueryui.com/demos/datepicker/)
		 * @param type The type of the element
		 */
		function(options, type)
		{
			if (type == "text") {
				this.datepicker(options);
			}
		}, $.isFunction($.fn.datepicker));

	$.dform.subscribe("autocomplete",
		/**
		 * Adds the autocomplete feature to a text element.
		 *
		 * @param options As specified in the [jQuery UI autotomplete documentation](http://jqueryui.com/demos/autotomplete/)
		 * @param type The type of the element
		 */
		function(options, type)
		{
			if (type == "text") {
				this.autocomplete(options);
			}
		}, $.isFunction($.fn.autocomplete));

	$.dform.subscribe("[post]",
		/**
		 * Post processing subscriber that adds jQuery UI styling classes to
		 * text, textarea, password and fieldset elements as well
		 * as calling .button() on submit or button elements.
		 *
		 * Additionally, accordion and tabs elements will be initialized
		 * with their options.
		 *
		 * @param options All options that have been passed for creating the element
		 * @param type The type of the element
		 */
		function(options, type)
		{
			if (this.parents("form").hasClass("ui-widget"))
			{
				if ((type === "button" || type === "submit") && $.isFunction($.fn.button)) {
					this.button();
				}
				if (!!~$.inArray(type, [ "text", "textarea", "password",
						"fieldset" ])) {
					this.addClass("ui-widget-content ui-corner-all");
				}
			}
			if(type === "accordion" || type === "tabs") {
				this[type](_getOptions(type, options));
			}
		});
	
	$.dform.subscribe("[pre]",
		/**
		 * Add a preprocessing subscriber that calls .validate() on the form,
		 * so that we can add rules to the input elements. Additionally
		 * the jQuery UI highlight classes will be added to the validation
		 * plugin default settings if the form has the ui-widget class.
		 * 
		 * @param options All options that have been used for
		 * creating the current element.
		 * @param type The type of the *this* element
		 */
		function(options, type)
		{
			if(type == "form")
			{
				var defaults = {};
				if(this.hasClass("ui-widget"))
				{
					defaults = {
						highlight: function(input)
						{
							$(input).addClass("ui-state-highlight");
						},
						unhighlight: function(input)
						{
							$(input).removeClass("ui-state-highlight");
						}
					};
                }
                if (this.is("#metadataEditorForm")) {
                    // DataDock specific jquery.validate configuration
                    defaults = {
                        debug: true,
                        ignore: ".skip-validation",
                        onkeyup: false,
                        onfocusout: false,
                        onclick: false,
                        showErrors: function (errorMap, errorList) {
                            console.log(errorMap);
                            console.log(errorList);
                            var numErrors = this.numberOfInvalids();
                            if (numErrors) {
                                var validationMessage = numErrors === 1
                                    ? '1 field is missing or invalid, please correct this before submitting your data.'
                                    : numErrors +
                                    ' fields are missing or invalid, please correct this before submitting your data.';

                                $("#validation-messages").html(validationMessage);
                                var invalidFieldList = $('<ul />');
                                $.each(errorList,
                                    function(i) {
                                        var error = errorList[i];
                                        $('<li/>')
                                            .addClass('invalid-field')
                                            .appendTo(invalidFieldList)
                                            .text(error.message);
                                    });
                                $("#validation-messages").append(invalidFieldList);
                                $("#validation-messages").css("margin", "0.5em");
                                $("#validation-messages").show();
                                this.defaultShowErrors();
                            } else {
                                $("#validation-messages").hide();
                            }
                        },
                       /* invalidHandler: function (event, validator) {
                            // 'this' refers to the form
                            var errors = validator.numberOfInvalids();
                            if (errors) {
                                var message = errors === 1
                                    ? 'You missed 1 field. It has been highlighted'
                                    : 'You missed ' + errors + ' fields. They have been highlighted';
                                $("#validation-messages").html(message);
                                $("#validation-messages").show();
                            } else {
                                $("#validation-messages").hide();
                            }
                        },*/
                        errorPlacement: function (error, element) {
                            error.insertBefore(element);
                        },
                        highlight: function(element, errorClass, validClass) {
                            $(element).parents(".field").addClass(errorClass);
                        },
                        unhighlight: function(element, errorClass, validClass) {
                            $(element).parents(".field").removeClass(errorClass);
                        },
                        submitHandler: function (e) {
                            sendData(e);
                        }
                };
			    }
				if (typeof (options.validate) == 'object') {
					$.extend(defaults, options.validate);
				}
				this.validate(defaults);
			}
		}, $.isFunction($.fn.validate));

		/**
		 * Adds support for the jQuery validation rulesets.
		 * For types: text, password, textarea, radio, checkbox sets up rules through rules("add", rules) for validation plugin
		 * For type <form> sets up as options object for validate method of validation plugin
		 * For rules of types checkboxes and radiobuttons you should use this subscriber for type form (to see example below)
		 *
		 * @param options
		 * @param type
		 */
	$.dform.subscribe("validate", function(options, type)
		{
			if (type != "form") {
				this.rules("add", options);
			}
		}, $.isFunction($.fn.validate));

	$.dform.subscribe("ajax",
		/**
		 * If the current element is a form, it will be turned into a dynamic form
		 * that can be submitted asynchronously.
		 *
		 * @param options Options as specified in the [jQuery Form plugin documentation](http://jquery.malsup.com/form/#options-object)
		 * @param type The type of the element
		 */
		function(options, type)
		{
			if(type === "form")
			{
				this.ajaxForm(options);
			}
		}, $.isFunction($.fn.ajaxForm));

	$.dform.subscribe('html',
		/**
		 * Extends the html subscriber that will replace any string with it's translated
		 * equivalent using the jQuery Global plugin. The html content will be interpreted
		 * as an index string where the first part indicates the localize main index and
		 * every following a sub index using getValueAt.
		 *
		 * @param options The dot separated html string to localize
		 * @param type The type of the this element
		 */
		function(options, type)
		{
			if(typeof options === 'string') {
				var keys = options.split('.'),
					translated = Globalize.localize(keys.shift());
				if(translated = _get(keys, translated)) {
					$(this).html(translated);
				}
			}
		}, typeof Globalize !== 'undefined' && $.isFunction(Globalize.localize));

	$.dform.subscribe('options',
		/**
		 * Extends the options subscriber for using internationalized option
		 * lists.
		 *
		 * @param options Options as specified in the <jQuery Form plugin documentation at http://jquery.malsup.com/form/#options-object>
		 * @param type The type of the element.
		 */
		function(options, type)
		{
			if(type === 'select' && typeof(options) === 'string') {
				$(this).html('');
				var keys = options.split('.'),
					optlist = Globalize.localize(keys.shift());
				if(optlist = _get(keys, optlist)) {
					$(this).dform('run', 'options', optlist, type);
				}
			}
		}, typeof Globalize !== 'undefined' && $.isFunction(Globalize.localize));
})(jQuery);
