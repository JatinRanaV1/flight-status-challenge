export type FlightStatusCode = 'OnTime' | 'Delayed' | 'Cancelled' | 'Diverted' | 'Unknown';

export interface FlightStatusResult {
  flightNumber:       string;
  date:               string;
  status:             FlightStatusCode;
  scheduledDeparture: string;
  scheduledArrival:   string;
  actualDeparture:    string | null;
  actualArrival:      string | null;
  terminal:           string | null;
  gate:               string | null;
  delayReason:        string | null;
  message:            string | null;
  lastUpdatedUtc:     string;
  providerSource:     string;
}

export interface SearchState {
  result:  FlightStatusResult | null;
  error:   string | null;
  loading: boolean;
}
