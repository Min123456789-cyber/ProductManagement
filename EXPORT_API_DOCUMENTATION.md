# Teacher Export API Documentation

This document describes the export functionality for teacher data in different formats.

## Overview

The export API allows you to export teacher data in multiple formats:
- CSV (Comma-Separated Values)
- Excel (XLSX)
- JSON (JavaScript Object Notation)
- XML (Extensible Markup Language)

## Endpoints

### 1. Generic Export Endpoint

**POST** `/api/teachers/export`

Exports teacher data with custom format and filter options.

**Request Body:**
```json
{
  "format": "csv", // "csv", "excel", "json", "xml"
  "fileName": "custom_filename", // optional
  "includeHeaders": true, // optional, default: true
  "filter": {
    "searchKeyword": "john" // optional
  }
}
```

### 2. CSV Export

**GET** `/api/teachers/export/csv`

**Query Parameters:**
- `searchKeyword` (optional): Filter teachers by name, email, or department
- `fileName` (optional): Custom file name without extension

**Example:**
```
GET /api/teachers/export/csv?searchKeyword=john&fileName=teachers_list
```

### 3. Excel Export

**GET** `/api/teachers/export/excel`

**Query Parameters:**
- `searchKeyword` (optional): Filter teachers by name, email, or department
- `fileName` (optional): Custom file name without extension

**Example:**
```
GET /api/teachers/export/excel?searchKeyword=math&fileName=math_teachers
```

### 4. JSON Export

**GET** `/api/teachers/export/json`

**Query Parameters:**
- `searchKeyword` (optional): Filter teachers by name, email, or department
- `fileName` (optional): Custom file name without extension

**Example:**
```
GET /api/teachers/export/json?fileName=teachers_data
```

### 5. XML Export

**GET** `/api/teachers/export/xml`

**Query Parameters:**
- `searchKeyword` (optional): Filter teachers by name, email, or department
- `fileName` (optional): Custom file name without extension

**Example:**
```
GET /api/teachers/export/xml?searchKeyword=science&fileName=science_teachers
```

## Response

All endpoints return a file download response with the appropriate content type:

- CSV: `text/csv`
- Excel: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- JSON: `application/json`
- XML: `application/xml`

## Data Fields

The exported data includes the following fields:

| Field | Description |
|-------|-------------|
| ID | Unique identifier for the teacher |
| First Name | Teacher's first name |
| Middle Name | Teacher's middle name (optional) |
| Last Name | Teacher's last name |
| Full Name | Complete name (First + Middle + Last) |
| Email | Teacher's email address |
| Phone | Teacher's phone number |
| Department ID | Unique identifier for the department |
| Department Name | Name of the department |
| Creation Time | When the teacher record was created |
| Last Modification Time | When the teacher record was last modified |

## Error Handling

The API returns appropriate HTTP status codes:

- `200 OK`: Export successful
- `400 Bad Request`: Invalid format or request parameters
- `404 Not Found`: No data found to export
- `500 Internal Server Error`: Server error during export

## Usage Examples

### Using cURL

```bash
# Export all teachers to CSV
curl -X GET "https://localhost:44312/api/teachers/export/csv" -H "accept: text/csv"

# Export filtered teachers to Excel
curl -X GET "https://localhost:44312/api/teachers/export/excel?searchKeyword=math" -H "accept: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"

# Export with custom filename
curl -X GET "https://localhost:44312/api/teachers/export/json?fileName=my_teachers" -H "accept: application/json"
```

### Using JavaScript/Fetch

```javascript
// Export to CSV
fetch('/api/teachers/export/csv?searchKeyword=john')
  .then(response => response.blob())
  .then(blob => {
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'teachers.csv';
    a.click();
  });

// Export to Excel
fetch('/api/teachers/export/excel?fileName=teachers_report')
  .then(response => response.blob())
  .then(blob => {
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'teachers_report.xlsx';
    a.click();
  });
```

## Notes

- The search functionality filters by first name, last name, email, and department name
- File names are automatically generated if not provided (format: `teachers_export_YYYYMMDD_HHMMSS`)
- Excel files include formatted headers with styling
- CSV files use UTF-8 encoding and comma as delimiter
- JSON files are formatted with indentation for readability
- XML files include proper XML declaration and structure
