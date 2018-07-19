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


$(function() {
    $("#metadataEditorForm").toggle();

    $("#fileSelectTextBox").click(function(e){
        $("input:file", $(e.target).parents()).click();
    });
    
    $("#fileSelectButton").click(function(e){
        $("input:file", $(e.target).parents()).click();
    });

    /*
    $("#fileSelect").on('change', function (e) {
        var caption = '';
        if(e.target.files[0]){
            var kb = 1;
            var name = e.target.files[0].name;
            var size = e.target.files[0].size;
            if (size > 1024) {
                kb = size / 1024;
            }
            caption = name + ' (' + Math.round(kb) + 'KB)';
        }

        $('#fileSelectTextBox', $(e.target).parent()).val(caption);
    });
    */

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

    $("#fileSelect").on("change", function()
    {
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
            // todo check max file size if (file.size > 1024 * 1024 * 4)

            if (file.size > 1024 * 1024 * 10)
            {
                config.step = stepFn;
            }

            start = performance.now();
            Papa.parse(file, config);
        }
        else
        {
            //start = performance.now();
            //var results = Papa.parse(txt, config);
            //console.log("Synchronous parse results:", results);
            console.error("No file selected");
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

});

function constructCsvwMetadata() {
    var csvw = {};
    csvw["@context"] = "http://www.w3.org/ns/csvw";

    csvw["url"] = $("#datasetId").val();

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

    csvw["aboutUrl"] = $("#datasetIdentifier").val();

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

    var column = {};
    column["name"] = columnName;
    if (skip) {
        column["suppressOutput"] = true;
    } else {

        var columnTitle = $(colId + "_title").val();
        column["titles"] = [columnTitle];

        column["datatype"] = $(colId + "_datatype").val();

        column["propertyUrl"] = $(colId + "_property_url").val();
    }
    return column;
}

function sendData(e){
    console.log("sendData", e);  

    $("#step2").removeClass("active");
    $("#step3").addClass("active");
    
    var formData = new FormData();
    formData.append('ownerId', ownerId); // global variable set on Import.cshtml
    formData.append('repoId', repoId); // global variable set on Import.cshtml
    formData.append('file', csvFile, filename);
    formData.append('filename', filename);
    formData.append('metadata', JSON.stringify(constructCsvwMetadata()));
    formData.append('showOnHomePage', JSON.stringify($("#showOnHomepage").prop("checked")));
    formData.append('saveAsSchema', JSON.stringify($("#saveAsTemplate").prop("checked")));
    formData.append('addToExisting', JSON.stringify($("#addToExistingData").prop("checked")));
    
    console.log("formData.......");
    console.log(JSON.stringify(formData));

    var apiOptions ={
        url: "/api/data",
        type: "POST",
        data: formData,
        processData: false,
        contentType: false,
        success: function(r) {
            sendDataSuccess();
        },
        error: function(r) {
            sendDataFailure(r);
        }
    };

    console.log(apiOptions);

    $.ajax(apiOptions);

    return false;
}

function sendDataSuccess() {
    var prefix = getPrefix();
    var jobsUrl = prefix + "/jobs";
    window.location.href = jobsUrl;
}

function sendDataFailure(response) {
    if (response) {
        var responseMsg = response["responseText"];
        $("#error-messages ul li:last")
            .append("<li><span>Publish data API has reported an error: " + responseMsg + "</span></li>");

    } else {
        $("#error-messages ul li:last").append("<li><span>Publish data API has resulted in an unspecified error</span></li>");
    }
}

function getPrefix() {
    return dataDockBaseUrl + "/" + ownerId + "/" + repoId;
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
        console.log(results, results.data[0]);
        parserHandle.pause();
        return;
    }

    if (printStepChecked)
        console.log(results, results.data[0]);
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
    console.log("Finished input (async). Time:", end-start, arguments);
    console.log("Rows:", rows, "Stepped:", stepped, "Chunks:", chunks);
    buildFormTemplate();
}
//end papaparse

//jqeury.dform
function buildFormTemplate(){
    console.log("Header:", header, "Columns:", columnCount);

    columnSet = [];

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
                "value": filename,
                "validate": {
                    "required": true,
                    "minlength": 2,
                    "messages": {
                        "required": "Please enter a title",
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
                "type": "text"
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
                "type": "text"
            }
        }
    ];

    var columnDefinitionsTableElements = [];

    columnDefinitionsTableElements.push(
        { "type" : "thead",
            "html" :
                [{ "type" : "tr",
                "html" : [
                    {
                        "type" : "th",
                        "html" : "Title"
                    },
                    {
                        "type" : "th",
                        "html" : "DataType"
                    }
                    ,
                    {
                        "type" : "th",
                        "html" : "Suppress In Output"
                    }
                ] }
            ]}
    );
    for (var colIdx = 0; colIdx < columnCount; colIdx++) {

        var trElements = [];
        var colTitle = header[colIdx];
        var colName = slugify(colTitle, "_", "_", "lowercase");

        columnSet.push(colName);

        var titleField = {
            name: colName + "_title",
            id: colName + "_title",
            type: "text",
            placeholder: "",
            value: colTitle,
            "validate": {
                "required": true,
                "messages": {
                    "required": "Column '" + colName + "' is missing a title"
                }
            }
        };
        var tdTitle = { "type" : "td", "html": titleField};
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
        var tdDatatype = { "type" : "td", "html": datatypeField};
        trElements.push(tdDatatype);

        var suppressField = {
            name: colName + "_suppress",
            id: colName + "_suppress",
            type: "checkbox",
            "class": "center aligned"
        };
        var tdSuppress = { "type" : "td", "html": suppressField};
        trElements.push(tdSuppress);

        var tr = { "type" : "tr", "html": trElements};
        columnDefinitionsTableElements.push(tr);
    }
    var columnDefinitionsTable = { "type": "table", "html": columnDefinitionsTableElements, "class": "ui celled table" };

    var datasetIdDefaultValue = getPrefix() + "/id/dataset/" + slugify(filename, "", "", "camelCase");
    
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
                                    "value": datasetIdDefaultValue,
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
        { "type" : "thead",
            "html" :
                [{ "type" : "tr",
                    "html" : [
                        {
                            "type" : "th",
                            "html": "Construct individual record identifiers from which column's values?"
                        }
                    ] }
                ]}
    );
    var prefix = getPrefix();
    var rowIdentifier = prefix +"/id/resource/acsv.csv/row_{_row}";
    var identifierOptions = {};
    identifierOptions[rowIdentifier] = "Row Number";

    for (var colIdx = 0; colIdx < columnCount; colIdx++) {
        var colTitle = header[colIdx];
        var colName = slugify(colTitle, "_", "_", "lowercase");
        var colIdentifier = prefix + "/id/resource/acsv.csv/" + colName + "/{" + colName + "}";
        identifierOptions[colIdentifier] = colTitle;
    }
    identifierTableElements.push(
        { "type" : "tbody",
            "html" :
                [{ "type" : "tr",
                    "html" : [
                        {
                            "type" : "td",
                            "html" : [
                                {
                                    name: "datasetIdentifier",
                                    id: "datasetIdentifier",
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
                ]}
    );
    var identifierTable = {"type" : "table", "html": identifierTableElements, "class": "ui celled table"};

    var predicateTableElements = [];
    predicateTableElements.push(
        { "type" : "thead",
            "html" :
                [{ "type" : "tr",
                    "html" : [
                        {
                            "type" : "th",
                            "html" : "Column"
                        },
                        {
                            "type" : "th",
                            "html" : "Property (URL)"
                        }
                    ] }
                ]}
    );
    for (var colIdx = 0; colIdx < columnCount; colIdx++) {

        var trElements = [];
        var colTitle = header[colIdx];
        var colName = slugify(colTitle, "_", "_", "lowercase");

        var titleDiv = {
            type: "div",
            html: colTitle
        };
        var tdTitle = { "type" : "td", "html": titleDiv};
        trElements.push(tdTitle);

        var predicate = prefix + "/id/definition/" + colName;
        var predicateField = {
            name: colName + "_property_url",
            id: colName + "_property_url",
            type: "text",
            placeholder: "",
            "value": predicate,
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
        var predDiv = {"type": "div", "class": "field", "html": predicateField};
        var tdPredicate = { "type" : "td", "html": predDiv};
        trElements.push(tdPredicate);

        var tr = { "type" : "tr", "html": trElements};
        predicateTableElements.push(tr);
    }
    var predicateTable = {"type" : "table", "html": predicateTableElements, "class": "ui celled table"};

    var identifierSection = [];
    identifierSection.push(
        { "type" : "div",
            "html" :
                [dsIdTable, identifierTable]}
    );

    var advancedSection = [];
    advancedSection.push(
        { "type" : "div",
            "html" :
                [predicateTable]}
    );

    var showOnHomepage = {
        "type": "div",
        "class": "ui checkbox",
        "html" : {
            "type": "checkbox",
            "name": "showOnHomepage",
            "id": "showOnHomepage",
            "caption": "Include my published dataset on DataDock homepage and search",
            "value": true
        }
    };
    var addToData = {
        "type": "div",
        "class": "ui checkbox",
        "html" : {
            "type": "checkbox",
            "name": "addToExistingData",
            "id": "addToExistingData",
            "caption": "Add to existing data if dataset already exists (default is to overwrite existing data)",
            "value": false
        }
    };
    var saveAsTemplate = {
        "type": "div",
        "class": "ui checkbox",
        "html" : {
            "type": "checkbox",
            "name": "saveAsTemplate",
            "id": "saveAsTemplate",
            "caption": "Save this information as a template for future imports",
            "value": false
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

    var submitButton = {
        "type": "div",
        "class": "ui center aligned container",
        "html": [
            hiddenDivider, {
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
                    "html": datasetVoidFields
                },
                {
                    "type": "div",
                    "id": "columnDefinitions",
                    "html": columnDefinitionsTable
                },
                {
                    "type": "div",
                    "id": "identifier",
                    "html": identifierSection
                },
                {
                    "type": "div",
                    "id": "advanced",
                    "html": advancedSection
                },
                {
                    "type": "div",
                    "id": "preview",
                    "html": constructPreviewTabContent()
                }
            ]
        }
    };
    var mainForm = {
        "type": "div",
        "class": "ui stackable two column grid container",
        "html": [tabs, tabsContent]
    };

    var formTemplate = {
        "class": "ui form",
        "method": "POST"
    };
    formTemplate.html = [mainForm, configCheckboxes, submitButton];
    console.log(formTemplate);
    $("#metadataEditorForm").dform(formTemplate);
    $("#metadataEditorForm").toggle();
    $("#fileSelector").toggle();

    $("#step1").removeClass("active");
    $("#step2").addClass("active");

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
//end jquery.dform

//helper functions
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
//end helper functions

//ui functions
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

function sniffDatatype(){

}

function chooseFile() {
    var prefix = getPrefix();
    if (prefix) {
        window.location.href = prefix + "/import";
    }
}

