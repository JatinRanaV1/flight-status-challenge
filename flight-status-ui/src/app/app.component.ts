import { Component } from '@angular/core';
import { SearchFormComponent } from './components/search-form/search-form.component';
import { ResultCardComponent } from './components/result-card/result-card.component';
import { FlightStatusResult, SearchState } from './models/flight-status.model';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [SearchFormComponent, ResultCardComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  result:       FlightStatusResult | null = null;
  errorMessage: string | null = null;

  onStateChange(state: SearchState): void {
    this.result       = state.result;
    this.errorMessage = state.error;
  }
}
