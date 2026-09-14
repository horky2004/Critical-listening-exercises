import { Component, type ErrorInfo, type ReactNode } from "react";
import { strings } from "../lib/strings";
import { Button } from "./Button";

type Props = { children: ReactNode };
type State = { error: Error | null };

export class ErrorBoundary extends Component<Props, State> {
  state: State = { error: null };

  static getDerivedStateFromError(error: Error): State {
    return { error };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error(error, info.componentStack);
  }

  render() {
    if (!this.state.error) {
      return this.props.children;
    }

    return (
      <div className="mx-auto max-w-lg space-y-4 p-8" role="alert">
        <p className="text-lg font-semibold tracking-tight">{strings.unexpectedError}</p>
        <p className="text-sm text-muted">{this.state.error.message || strings.error}</p>
        <Button onClick={() => window.location.assign("/")}>{strings.retry}</Button>
      </div>
    );
  }
}
