var stepped = 0, chunks = 0, rows = 0, columnCount = 0;
var start, end;
var parser;
var pauseChecked = false;
var printStepChecked = false;
var header;

var dataDockBaseUrl = 'http://datadock.io';
var ownerId = 'jennet';
var repoId = 'demo-repo';
var apiUrl = "https://dde7858c-3da0-4783-968b-f9a3ea1d6e05.mock.pstmn.io/data";

var csvData;
var formData = new FormData();

$(function() {
    $("#metadataEditorForm").toggle();

    $("#fileSelectTextBox").click(function(e){
        $('input:file', $(e.target).parents()).click();
    });
    $("#fileSelectButton").click(function(e){
        $('input:file', $(e.target).parents()).click();
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

    // add types to dForm
    $.dform.addType("tabs", function(options) {
        return $(this).addClass('');
    });
    $.dform.addType("publishButton", function(options) {
        return $("<button type='button'>").dform('attr', options).html("Publish Data");
    });

    $.dform.subscribe("publish", function(options, type) {
        if(type === "publishButton") {
            this.click(function() {
                sendData(options);
                return false;
            });
        }
    });

    $('#fileSelect').on('change', function()
    {
        stepped = 0;
        chunks = 0;
        rows = 0;

                // todo only deal with a single file
        // todo check that the file input can only select CSV
        var files = $('#fileSelect')[0].files;
        var config = buildConfig();

        pauseChecked = $('#step-pause').prop('checked');
        printStepChecked = $('#print-steps').prop('checked');


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

    $('#submit-unparse').click(function()
    {
        var input = $('#input').val();
        var delim = $('#delimiter').val();
        var header = $('#header').prop('checked');

        var results = Papa.unparse(input, {
            delimiter: delim,
            header: header,
        });

        console.log("Unparse complete!");
        console.log("--------------------------------------");
        console.log(results);
        console.log("--------------------------------------");
    });

    $('#insert-tab').click(function()
    {
        $('#delimiter').val('\t');
    });
});

function sendData(options){
    console.log('sendData', options);
    var form = $('form');


    $.each($("input[name$='_title']", form ), function(i, fields){
        formData.append($(fields).attr('name'), $(fields).val());
    });

    // Display the key/value pairs
    for (var pair of formData.entries()) {
        console.log(pair[0]+ ', ' + pair[1]);
    }

    var apiOptions ={
        url: apiUrl,
        type: 'POST',
        enctype: 'multipart/form-data',
        data: formData,
        processData: false,
        contentType: false,
        success: function(data)
        {
            console.log(data);
        },
        error: function(data)
        {
            console.error(data);
        }
    };

    console.log(apiOptions);

    // $.ajax(apiOptions  );


    return false;
}

function getPrefix() {
    return dataDockBaseUrl + '/' + ownerId + '/' + repoId;
}

function buildConfig()
{
    var config = {
        header: false,
        preview: 0,
        delimiter: $('#delimiter').val(),
        newline: getLineEnding(),
        comments: $('#comments').val(),
        encoding: $('#encoding').val(),
        worker: false,
        step: undefined,
        complete: completeFn,
        error: errorFn,
        download: false,
        skipEmptyLines: true,
        //chunk: $('#chunk').prop('checked') ? chunkFn : undefined,
        chunk: undefined,
        beforeFirstChunk: undefined,
    };
    return config;

    function getLineEnding()
    {
        if ($('#newline-n').is(':checked'))
            return "\n";
        else if ($('#newline-r').is(':checked'))
            return "\r";
        else if ($('#newline-rn').is(':checked'))
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
    if (!$('#stream').prop('checked')
        && !$('#chunk').prop('checked')
        && arguments[0]
        && arguments[0].data)
        rows = arguments[0].data.length;

    csvData = arguments[0].data;
    // arguments[0] .data [][] | .errors [] | meta (aborted, cursor, delimiter, linebreak, truncated)
    var file = arguments[1];
    if (csvData) {
        header = csvData[0];
        columnCount = header.length;
        formData.append(file.name, file);
    }
    console.log("Finished input (async). Time:", end-start, arguments);
    console.log("Rows:", rows, "Stepped:", stepped, "Chunks:", chunks);
    buildFormTemplate();
}

function buildFormTemplate(){
    console.log("Header:", header, "Columns:", columnCount);

    var datasetVoidFields = [
        {
            "name": "datasetTitle",
            "id": "datasetTitle",
            "caption": "Title",
            "type": "text",
            "validate" : {
                "required" : true,
                "minlength" : 2,
                "messages" : {
                    "required" : "Required input",
                }
            }
        },
        {
            "name": "datasetDescription",
            "id": "datasetDescription",
            "caption": "Description",
            "type": "text",
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
        var colName = slugify(colTitle);

        var titleField = {
            name: colName + '_title',
            id: colName + '_title',
            type: "text",
            placeholder: '',
            value: colTitle
        };
        var tdTitle = { "type" : "td", "html": titleField};
        trElements.push(tdTitle);

        var datatypeField = {
            name: colName + "_datatype",
            id: colName + "_datatype",
            type: "select",
            placeholder: '',
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
            name: colName + '_suppress',
            id: colName + '_suppress',
            type: "checkbox",
            "class": "center aligned"
        };
        var tdSuppress = { "type" : "td", "html": suppressField};
        trElements.push(tdSuppress);

        var tr = { "type" : "tr", "html": trElements};
        columnDefinitionsTableElements.push(tr);
    }
    var columnDefinitionsTable = {"type" : "table", "html": columnDefinitionsTableElements, "class": "ui celled table"};


    var identifierTableElements = [];
    identifierTableElements.push(
        { "type" : "thead",
            "html" :
                [{ "type" : "tr",
                    "html" : [
                        {
                            "type" : "th",
                            "html" : "Identifier"
                        }
                    ] }
                ]}
    );
    var prefix = getPrefix();
    var rowIdentifier = prefix +'/id/resource/acsv.csv/row_{_row}';
    var identifierOptions = { rowIdentifier : "Row Number" };

    for (var colIdx = 0; colIdx < columnCount; colIdx++) {
        var colTitle = header[colIdx];
        var colName = slugify(colTitle);
        var colIdentifier = prefix + '/id/resource/acsv.csv/' + colName + '/{' + colName + '}';
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
                                    name: 'identifier',
                                    id: 'identifier',
                                    type: "select",
                                    placeholder: '',
                                    options: identifierOptions
                                }
                            ]
                        }
                    ] }
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
        var colName = slugify(colTitle);

        var titleDiv = {
            type: "div",
            html: colTitle
        };
        var tdTitle = { "type" : "td", "html": titleDiv};
        trElements.push(tdTitle);

        var predicate = prefix + '/id/definition/' + colName;
        var predicateField = {
            name: colName + '_property_url',
            id: colName + '_property_url',
            type: "text",
            placeholder: '',
            "value": predicate,
            "class": "pred-field"
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
                [identifierTable]}
    );

    var advancedSection = [];
    advancedSection.push(
        { "type" : "div",
            "html" :
                [predicateTable]}
    );

    var showOnHomepage = {
        "type" : "div",
        "html" : {
            "type": "checkbox",
            "name": "showOnHomepage",
            "id": "showOnHomepage",
            "caption": "Include my published dataset on DataDock homepage and search",
            "value": true
        }
    };
    var addToData = {
        "type" : "div",
        "html" : {
            "type": "checkbox",
            "name": "addToExistingData",
            "id": "addToExistingData",
            "caption": "Add to existing data if dataset already exists (default is to overwrite existing data)",
            "value": false
        }
    };
    var saveAsTemplate = {
        "type" : "div",
        "html" : {
            "type": "checkbox",
            "name": "saveAsTemplate",
            "id": "saveAsTemplate",
            "caption": "Save this information as a template for future imports",
            "value": false
        }
    };
    var configCheckboxes = {
        "type": "div",
        "html": [addToData, showOnHomepage, saveAsTemplate]
    };

    var submitButton = {
        "type" : "div",
        "class": "ui buttons",
        "html" : {
            "type": "publishButton",
            "id": "publish",
            "class": "ui primary button large",
            "publish": "yo"
        }
    };

    //todo button
    //dataTypeFields.push({type:"submit", value: "Dymanic Form A-go-go"});


    var tabs = {
        "type" : "tabs",
        "entries" : [
            {
                "caption" : "Dataset Info",
                "id" : "datasetInfo",
                "id" : "datasetInfo",
                "html" : datasetVoidFields
            },
            {
                "caption" : "Column Definitions",
                "id" : "columnDefinitions",
                "html" : columnDefinitionsTable
            },
            {
                "caption" : "Identifier",
                "id" : "identifier",
                "html" : identifierSection
            },
            {
                "caption" : "Advanced",
                "id" : "advanced",
                "html" : advancedSection
            },
            {
                "caption" : "Data Preview",
                "id" : "preview",
                "html" : "TODO: data-tables preview html"
            }
        ]
    };

    var formTemplate = {
        "class": "ui form",
        "method": "POST"
    };
    formTemplate.html = [tabs, configCheckboxes, submitButton];
    $("#metadataEditorForm").dform(formTemplate);
    $("#metadataEditorForm").toggle();
    $("#fileSelector").toggle();


    function slugify(columnName){
        var fieldName = columnName.replace(/[^A-Z0-9]+/ig, "_").replace('__','_').toLowerCase();
        return fieldName;
    }
}

function sniffDatatype(){

}