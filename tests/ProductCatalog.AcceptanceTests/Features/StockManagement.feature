Feature: Stock management
  Sales channels and deliveries change a product's stock. The stock must stay correct when many
  requests arrive at the same moment, and it can never go below zero or past its maximum.

  Scenario: Selling reduces the stock
    Given a product "Lens" with 10 units in stock
    When 3 units of "Lens" are sold
    Then the request succeeds
    And "Lens" has 7 units in stock

  Scenario: Selling more than is available is rejected
    Given a product "Lens" with 3 units in stock
    When 4 units of "Lens" are sold
    Then the request is rejected with status 400 and error code "INSUFFICIENT_STOCK"
    And "Lens" has 3 units in stock

  Scenario: Simultaneous sales are all counted
    Given a product "Lens" with 100 units in stock
    When 50 customers each buy 1 unit of "Lens" at the same time
    Then all 50 requests succeed
    And "Lens" has 50 units in stock

  Scenario: Simultaneous sales never oversell
    Given a product "Lens" with 5 units in stock
    When 5 customers each buy 5 units of "Lens" at the same time
    Then 1 request succeeds and 4 are rejected with error code "INSUFFICIENT_STOCK"
    And "Lens" has 0 units in stock

  Scenario: Restocking increases the stock
    Given a product "Lens" with 10 units in stock
    When 5 units of "Lens" are added to stock
    Then the request succeeds
    And "Lens" has 15 units in stock

  Scenario: Simultaneous deliveries are all counted
    Given a product "Lens" with 0 units in stock
    When 50 deliveries each add 1 unit of "Lens" at the same time
    Then all 50 requests succeed
    And "Lens" has 50 units in stock

  Scenario: Restocking past the maximum stock is rejected
    Given a product "Lens" with 10 units in stock
    When 2147483647 units of "Lens" are added to stock
    Then the request is rejected with status 400 and error code "STOCK_LIMIT_EXCEEDED"
    And "Lens" has 10 units in stock

  Scenario: Selling a product that doesn't exist
    When 1 unit of an unknown product is sold
    Then the request is rejected with status 404 and error code "NOT_FOUND"
