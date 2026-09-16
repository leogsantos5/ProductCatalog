Feature: Product catalogue
  Products can be created, browsed page by page, and searched by name or by stock level.

  Scenario: Creating a product
    When a product "ZEISS Single Vision Lens" is created with price 89.90 and 500 units in stock
    Then the product is created with a 6-digit ID
    And "ZEISS Single Vision Lens" has 500 units in stock

  Scenario Outline: Products with invalid details are rejected
    When a product "<name>" is created with price <price> and <stock> units in stock
    Then the request is rejected with a validation error for "<field>"

    Examples:
      | name | price  | stock | field        |
      |      | 10.00  | 5     | Name         |
      | Lens | 0      | 5     | Price        |
      | Lens | 19.999 | 5     | Price        |
      | Lens | 10.00  | -1    | InitialStock |

  Scenario: A product without a price is rejected
    When a product "Lens" is created without a price
    Then the request is rejected as invalid

  Scenario: Retrieving a product that doesn't exist
    When an unknown product is retrieved
    Then the request is rejected with status 404 and error code "NOT_FOUND"

  Scenario: Searching by part of the name, ignoring case
    Given the following products exist:
      | name                     | price  | stock |
      | ZEISS Single Vision Lens | 89.90  | 500   |
      | ZEISS PhotoFusion X Lens | 129.50 | 80    |
      | ZEISS Cleaning Cloth     | 9.90   | 300   |
    When products are searched for "lens"
    Then the results are:
      | name                     |
      | ZEISS Single Vision Lens |
      | ZEISS PhotoFusion X Lens |

  Scenario: Search text is matched literally
    Given the following products exist:
      | name                     | price | stock |
      | ZEISS Single Vision Lens | 89.90 | 500   |
    When products are searched for "%"
    Then there are no results

  Scenario: Filtering by stock level
    Given the following products exist:
      | name              | price  | stock |
      | Out of stock lens | 99.00  | 0     |
      | Low stock lens    | 149.00 | 45    |
      | Normal stock lens | 249.00 | 120   |
      | Well stocked lens | 89.90  | 500   |
    When products with between 40 and 150 units in stock are requested
    Then the results are:
      | name              |
      | Low stock lens    |
      | Normal stock lens |

  Scenario: Filtering with a minimum above the maximum is rejected
    When products with between 150 and 40 units in stock are requested
    Then the request is rejected with a validation error for "Min"

  Scenario: Browsing the catalogue page by page
    Given 5 products exist
    When page 2 of the catalogue is requested with 2 products per page
    Then 2 products are returned
    And the catalogue reports 5 products across 3 pages

  Scenario: Every product appears exactly once across the pages
    Given 5 products exist
    When every page of the catalogue is requested with 2 products per page
    Then each product appears exactly once, in order of ID

  Scenario: Requesting too many products per page is rejected
    When page 1 of the catalogue is requested with 101 products per page
    Then the request is rejected with a validation error for "PageSize"
